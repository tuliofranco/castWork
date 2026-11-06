namespace Castwork.Domain.Entities.Ferramentas.Briefings;

/// <summary>
/// O formulário inicial, preenchido rapidamente no balcão.
/// </summary>
public sealed record FormularioBriefing(
    TipoJoia TipoJoia,
    string OQueOClienteQuer,
    string? Ocasiao,
    string? Materiais,
    string? Medidas,
    DateOnly? Prazo,
    string? Orcamento);
