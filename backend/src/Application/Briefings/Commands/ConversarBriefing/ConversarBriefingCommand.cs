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
/// Envia a fala do joalheiro à IA e grava a resposta (e o que as ferramentas fizeram) no briefing.
/// Sem texto, numa conversa vazia, a IA abre a conversa a partir do formulário.
/// Retorna o briefing atualizado, com a conversa.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ConversarBriefingCommand : ComandoDeProjeto, IRequest<BriefingDto>
{
    public const int TextoMaxLength = 4000;

    public string? Texto { get; set; }
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
        if (fala is null && briefing.Mensagens.Count > 0)
        {
            throw new RegraDeNegocioException("Escreva uma mensagem para continuar a conversa.");
        }

        var nomes = await ContextoDoBriefing.NomesAsync(unitOfWork, briefing.ProjetoId, cancellationToken);
        var contexto = ContextoDoBriefing.ParaIa(briefing, nomes, DateOnly.FromDateTime(relogio.GetLocalNow().DateTime));
        var historico = briefing.Mensagens.OrderBy(m => m.Ordem).Select(m => new MensagemDaConversa(m.Papel, m.Texto, m.Conteudo)).ToList();

        var ferramentas = new Ferramentas(unitOfWork, gerador, relogio, briefing);
        var novas = await assistente.ConversarAsync(contexto, historico, fala, ferramentas, cancellationToken);

        var agora = relogio.GetUtcNow();
        foreach (var mensagem in novas)
        {
            briefing.RegistrarMensagem(mensagem.Papel, mensagem.Texto, mensagem.Conteudo, agora);
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
    }
}
