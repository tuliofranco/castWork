using Castwork.Domain.Entities.Briefings;

namespace Castwork.Application.Abstractions;

/// <summary>
/// A IA que conduz a conversa do briefing. A implementação (Infra) conhece o provedor e o formato das
/// mensagens; a Application só guarda o que volta e executa as ferramentas pedidas.
/// </summary>
public interface IAssistenteBriefing
{
    /// <summary>
    /// Continua a conversa: acrescenta a fala do joalheiro, com as imagens anexadas (ou, sem fala e sem
    /// histórico, deixa a IA abrir), e devolve as mensagens novas, na ordem, para serem gravadas no briefing.
    /// <paramref name="imagens"/> traz todas as imagens da conversa, as antigas e as desta fala.
    /// </summary>
    Task<IReadOnlyList<MensagemDaConversa>> ConversarAsync(
        string contexto,
        IReadOnlyList<MensagemDaConversa> historico,
        string? falaDoJoalheiro,
        IReadOnlyList<Guid> imagensDaFala,
        IReadOnlyDictionary<Guid, ImagemDaConversa> imagens,
        IFerramentasDoBriefing ferramentas,
        CancellationToken cancellationToken);
}

/// <summary>
/// O que a IA pode fazer no sistema durante a conversa. Cada chamada devolve um texto curto para a IA.
/// </summary>
public interface IFerramentasDoBriefing
{
    string Refinar(RefinamentoBriefing refinamento);

    Task<string> GerarPdfAsync(CancellationToken cancellationToken);
}
