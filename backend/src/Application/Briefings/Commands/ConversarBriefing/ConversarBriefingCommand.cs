using Castwork.Application.Abstractions;
using Castwork.Application.Briefings.Queries.GetBriefing;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Briefings.Commands.ConversarBriefing;

/// <summary>
/// Envia a fala do joalheiro à IA, com fotos ou croquis anexados, e grava a resposta (e o que as
/// ferramentas fizeram) no briefing. Sem texto nem imagem, numa conversa vazia, a IA abre a conversa
/// a partir do formulário.
/// Retorna o briefing atualizado, com a conversa.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ConversarBriefingCommand : ComandoDeProjeto, IRequest<BriefingDto>
{
    public const int TextoMaxLength = 4000;
    public const int ImagensPorMensagem = 4;

    public string? Texto { get; set; }

    /// <summary>
    /// Imagens anexadas, em base64 (PNG, JPEG ou WebP, até 5 MB cada).
    /// </summary>
    public IReadOnlyList<string> Imagens { get; set; } = [];
}

public sealed class ConversarBriefingCommandHandler(
    IUnitOfWork unitOfWork,
    IAssistenteBriefing assistente,
    IGeradorDocumentoBriefing gerador,
    TimeProvider relogio,
    ISender sender)
    : IRequestHandler<ConversarBriefingCommand, BriefingDto>
{
    public async Task<BriefingDto> Handle(ConversarBriefingCommand request, CancellationToken cancellationToken)
    {
        var briefing = await unitOfWork.ObterDoProjetoAsync(request.ProjetoId, cancellationToken);
        var fala = string.IsNullOrWhiteSpace(request.Texto) ? null : request.Texto.Trim();
        if (fala is null && request.Imagens.Count == 0 && briefing.Mensagens.Count > 0)
        {
            throw new RegraDeNegocioException("Escreva uma mensagem ou anexe uma imagem para continuar a conversa.");
        }

        var agora = relogio.GetUtcNow();
        var repositorio = unitOfWork.GetRepository<ImagemBriefing>();

        var novasImagens = request.Imagens.Select(i => ImagemBriefing.Criar(briefing.Id, Convert.FromBase64String(i), agora)).ToList();

        // A IA relê a conversa inteira a cada turno, então as imagens antigas também vão.
        var imagens = (await repositorio.FindBy(i => i.BriefingId == briefing.Id)
                .Select(i => new ImagemDaConversa(i.Chave, i.Tipo, i.Conteudo))
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .Concat(novasImagens.Select(i => new ImagemDaConversa(i.Chave, i.Tipo, i.Conteudo)))
            .ToDictionary(i => i.Chave);
        var imagensDaFala = novasImagens.Select(i => i.Chave).ToList();
        novasImagens.ForEach(repositorio.Adicionar);

        var nomes = await ContextoDoBriefing.NomesAsync(unitOfWork, briefing.ProjetoId, cancellationToken);
        var contexto = ContextoDoBriefing.ParaIa(briefing, nomes, DateOnly.FromDateTime(relogio.GetLocalNow().DateTime));
        var historico = briefing.Mensagens.OrderBy(m => m.Ordem).Select(m => new MensagemDaConversa(m.Papel, m.Texto, m.Conteudo)).ToList();

        var ferramentas = new Ferramentas(unitOfWork, gerador, relogio, briefing);
        var novas = await assistente.ConversarAsync(contexto, historico, fala, imagensDaFala, imagens, ferramentas, cancellationToken);

        // As imagens pertencem à fala do joalheiro, a primeira mensagem do turno.
        for (var i = 0; i < novas.Count; i++)
        {
            briefing.RegistrarMensagem(novas[i].Papel, novas[i].Texto, novas[i].Conteudo, agora, i == 0 ? imagensDaFala : null);
        }

        await unitOfWork.SalvarAsync(cancellationToken);
        return (await sender.Send(new GetBriefingQuery { ProjetoId = request.ProjetoId }, cancellationToken))!;
    }

    /// <summary>
    /// Ferramentas da IA ligadas a este briefing: alteram o agregado já carregado, salvo no fim do command.
    /// </summary>
    private sealed class Ferramentas(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio, Briefing briefing)
        : IFerramentasDoBriefing
    {
        public string Refinar(RefinamentoBriefing refinamento)
        {
            try
            {
                briefing.Refinar(refinamento);
                return "Briefing atualizado.";
            }
            catch (ValorInvalidoException ex)
            {
                // Volta para a IA corrigir, em vez de derrubar a conversa.
                return $"Não foi possível atualizar: {ex.Message}";
            }
        }

        public async Task<string> GerarPdfAsync(CancellationToken cancellationToken)
        {
            var documento = await GeracaoDePdf.GerarAsync(unitOfWork, gerador, relogio, briefing, cancellationToken);
            return $"PDF gerado ({documento.NomeArquivo}). Ele aparece na lista de documentos do briefing para baixar.";
        }
    }
}

public sealed class ConversarBriefingCommandValidator : ValidatorBase<ConversarBriefingCommand>
{
    public ConversarBriefingCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Texto).MaximumLength(ConversarBriefingCommand.TextoMaxLength).WithName("Mensagem");
        RuleFor(c => c.Imagens).Must(i => i.Count <= ConversarBriefingCommand.ImagensPorMensagem)
            .WithMessage($"Anexe até {ConversarBriefingCommand.ImagensPorMensagem} imagens por mensagem.");
        RuleForEach(c => c.Imagens).Must(EhBase64).WithMessage("Imagem inválida.");
    }

    private static bool EhBase64(string valor) => !string.IsNullOrEmpty(valor) && Convert.TryFromBase64String(valor, new byte[valor.Length], out _);
}
