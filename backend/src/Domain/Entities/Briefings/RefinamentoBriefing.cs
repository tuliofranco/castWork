namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Campos que a conversa consolidou. Nulo significa "não mudou".
/// </summary>
public sealed record RefinamentoBriefing(
    string? OQueSeraFeito = null,
    string? Materiais = null,
    string? Medidas = null,
    string? Acabamento = null,
    string? Pedras = null,
    string? Fecho = null,
    DateOnly? Prazo = null,
    string? Orcamento = null,
    string? Observacoes = null);
