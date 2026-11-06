using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Exceptions;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Envia a fala do joalheiro (e as imagens) à IA da ferramenta. A IA trabalha no arquivo atual pelo MCP
/// da ferramenta; se ela mudar o arquivo, a nova versão é gravada e a mensagem leva o cartão da ação.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ConversarFerramentaCommand : ComandoDeFerramenta, IRequest<ConversaFerramentaDto>
{
    public const int TextoMaxLength = 4000;
    public const int ImagensPorMensagem = 4;

    public string? Texto { get; set; }

    /// <summary>
    /// Imagens anexadas, em base64 (PNG, JPEG ou WebP, até 5 MB cada).
    /// </summary>
    public IReadOnlyList<string> Imagens { get; set; } = [];
}

public sealed class ConversarFerramentaCommandHandler(
    IUnitOfWork unitOfWork,
    IAssistenteFerramenta assistente,
    IPainelDoTurno painel,
    CatalogoDeFerramentas catalogo,
    TimeProvider relogio)
    : IRequestHandler<ConversarFerramentaCommand, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(ConversarFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var texto = string.IsNullOrWhiteSpace(request.Texto) ? null : request.Texto.Trim();
        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDaVersao.ObterOuCriarAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, agora, cancellationToken);

        // Sem fala nem imagem, só a abertura: a IA da ferramenta começa o chat novo, se a ferramenta abre sozinha
        // (o briefing resume o formulário e pergunta o que falta).
        var ferramenta = catalogo.Obter(request.Ferramenta);
        if (texto is null && request.Imagens.Count == 0 && !(ferramenta.AbreSozinha && !conversa.MensagensDoChat.Any()))
        {
            throw new RegraDeNegocioException("Escreva uma mensagem ou anexe uma imagem.");
        }

        var arquivo = await ConversaDaVersao.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        var (contexto, locais) = await ferramenta.PrepararAsync(request.ProjetoId, request.VersaoId, cancellationToken);

        var repositorioDeImagens = unitOfWork.GetRepository<ImagemConversa>();
        var novas = request.Imagens.Select(i => ImagemConversa.Criar(conversa.Id, Convert.FromBase64String(i), agora)).ToList();

        // A IA relê a conversa inteira a cada turno, então as imagens antigas também vão.
        var imagens = (await repositorioDeImagens.FindBy(i => i.ConversaId == conversa.Id)
                .Select(i => new ImagemDaConversa(i.Chave, i.Tipo, i.Conteudo))
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .Concat(novas.Select(i => new ImagemDaConversa(i.Chave, i.Tipo, i.Conteudo)))
            .ToDictionary(i => i.Chave);

        // A fala (e as imagens dela) vai para o banco já: quem sai da tela e volta vê a mensagem e a IA trabalhando.
        var fala = conversa.RegistrarFala(texto, [.. novas.Select(i => i.Chave)], agora);
        novas.ForEach(repositorioDeImagens.Adicionar);
        await unitOfWork.SalvarAsync(cancellationToken);

        // Só o chat em andamento vai para a IA: um chat novo começa do zero.
        var historico = conversa.MensagensDoChat.Where(m => m != fala).Select(m => new MensagemDaConversa(m.Papel, m.Texto, m.Conteudo)).ToList();

        // Daqui em diante o turno vai até o fim mesmo que o joalheiro saia da tela: a resposta fica guardada.
        // Enquanto isso, a tela acompanha o pensamento e os passos pelo painel do turno.
        TurnoDaFerramenta turno;
        painel.Comecar(conversa.Id);
        try
        {
            turno = await assistente.ConversarAsync(
                new PedidoAFerramenta(
                    request.Ferramenta,
                    contexto,
                    historico,
                    texto,
                    [.. novas.Select(i => i.Chave)],
                    imagens,
                    arquivo is { EmBranco: false } ? arquivo.Conteudo : null,
                    evento => painel.Publicar(conversa.Id, evento),
                    locais),
                CancellationToken.None);
        }
        catch
        {
            painel.Terminar(conversa.Id);
            conversa.DescartarFala(fala);
            novas.ForEach(repositorioDeImagens.Remover);
            await unitOfWork.SalvarAsync(CancellationToken.None);
            throw;
        }

        foreach (var gerada in turno.ImagensGeradas)
        {
            repositorioDeImagens.Adicionar(ImagemConversa.Criar(conversa.Id, gerada.Conteudo, agora, gerada.Chave));
        }

        var arquivoId = arquivo is { EmBranco: false } ? arquivo.Id : (long?)null;
        if (turno.ArquivoNovo is { } conteudo)
        {
            var novo = ArquivoFerramenta.Criar(conversa.Id, conteudo, OrigemArquivo.Ia, agora);
            unitOfWork.GetRepository<ArquivoFerramenta>().Adicionar(novo);
            await unitOfWork.SalvarAsync(CancellationToken.None);
            arquivoId = novo.Id;
        }

        // A primeira mensagem do turno é a própria fala, com o conteúdo como a IA a recebeu.
        conversa.ConfirmarFala(fala, turno.Mensagens[0].Conteudo);
        foreach (var mensagem in turno.Mensagens.Skip(1))
        {
            var acao = mensagem.Acao is null || turno.ArquivoNovo is null ? null : new AcaoDaIa(mensagem.Acao, arquivo?.Id ?? 0);
            var detalhes = mensagem.Passos is null ? null : new DetalhesDoTurno(mensagem.Pensamento, mensagem.Passos, turno.Consumo?.Total);
            conversa.RegistrarMensagem(mensagem.Papel, mensagem.Texto, mensagem.Conteudo, agora, mensagem.Imagens, acao, detalhes);
        }

        if (turno.Consumo is { } consumo)
        {
            unitOfWork.GetRepository<ConsumoDaIa>().Adicionar(ConsumoDaIa.Registrar(
                request.Ferramenta, conversa.Id, consumo.Modelo, consumo.Chamadas, consumo.Entrada, consumo.Saida, consumo.CacheLeitura, consumo.CacheEscrita, agora));
        }

        await unitOfWork.SalvarAsync(CancellationToken.None);

        // Só agora: quem acompanha recebe o fim depois que a resposta já está gravada e pode ser lida.
        painel.Terminar(conversa.Id);
        return GetConversaFerramentaQueryHandler.Mapear(conversa, arquivoId, processando: false);
    }
}

public sealed class ConversarFerramentaCommandValidator : ValidatorBase<ConversarFerramentaCommand>
{
    public ConversarFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork)
    {
        this.ValidarFerramenta(UnitOfWork, catalogo);
        RuleFor(c => c.Texto).MaximumLength(ConversarFerramentaCommand.TextoMaxLength).WithName("Mensagem");
        RuleFor(c => c.Imagens).Must(i => i.Count <= ConversarFerramentaCommand.ImagensPorMensagem)
            .WithMessage($"Anexe até {ConversarFerramentaCommand.ImagensPorMensagem} imagens por mensagem.");
        RuleForEach(c => c.Imagens).Must(EhBase64).WithMessage("Imagem inválida.");
    }

    private static bool EhBase64(string valor) => !string.IsNullOrEmpty(valor) && Convert.TryFromBase64String(valor, new byte[valor.Length], out _);
}
