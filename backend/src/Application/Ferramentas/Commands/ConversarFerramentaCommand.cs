using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Application.Ferramentas.Queries;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Ferramentas.Commands;

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

public sealed class ConversarFerramentaCommandHandler(IUnitOfWork unitOfWork, IAssistenteFerramenta assistente, TimeProvider relogio)
    : IRequestHandler<ConversarFerramentaCommand, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(ConversarFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var texto = string.IsNullOrWhiteSpace(request.Texto) ? null : request.Texto.Trim();
        if (texto is null && request.Imagens.Count == 0)
        {
            throw new RegraDeNegocioException("Escreva uma mensagem ou anexe uma imagem.");
        }

        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDaVersao.ObterOuCriarAsync(unitOfWork, request.VersaoId, request.Ferramenta, agora, cancellationToken);
        var arquivo = await ConversaDaVersao.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        var contexto = await ContextoDaFerramenta.MontarAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);

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
        var historico = conversa.Mensagens.Where(m => m != fala).OrderBy(m => m.Ordem).Select(m => new MensagemDaConversa(m.Papel, m.Texto, m.Conteudo)).ToList();

        // Daqui em diante o turno vai até o fim mesmo que o joalheiro saia da tela: a resposta fica guardada.
        TurnoDaFerramenta turno;
        try
        {
            turno = await assistente.ConversarAsync(
                new PedidoAFerramenta(request.Ferramenta, contexto, historico, texto, [.. novas.Select(i => i.Chave)], imagens, arquivo is { EmBranco: false } ? arquivo.Conteudo : null),
                CancellationToken.None);
        }
        catch
        {
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
            conversa.RegistrarMensagem(mensagem.Papel, mensagem.Texto, mensagem.Conteudo, agora, mensagem.Imagens, acao);
        }

        await unitOfWork.SalvarAsync(CancellationToken.None);
        return GetConversaFerramentaQueryHandler.Mapear(conversa, arquivoId, processando: false);
    }
}

public sealed class ConversarFerramentaCommandValidator : ValidatorBase<ConversarFerramentaCommand>
{
    public ConversarFerramentaCommandValidator(IUnitOfWork unitOfWork, IAssistenteFerramenta assistente)
        : base(unitOfWork)
    {
        this.ValidarFerramenta(UnitOfWork, assistente);
        RuleFor(c => c.Texto).MaximumLength(ConversarFerramentaCommand.TextoMaxLength).WithName("Mensagem");
        RuleFor(c => c.Imagens).Must(i => i.Count <= ConversarFerramentaCommand.ImagensPorMensagem)
            .WithMessage($"Anexe até {ConversarFerramentaCommand.ImagensPorMensagem} imagens por mensagem.");
        RuleForEach(c => c.Imagens).Must(EhBase64).WithMessage("Imagem inválida.");
    }

    private static bool EhBase64(string valor) => !string.IsNullOrEmpty(valor) && Convert.TryFromBase64String(valor, new byte[valor.Length], out _);
}
