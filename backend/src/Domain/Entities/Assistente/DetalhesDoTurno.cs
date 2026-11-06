namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// O que acompanha a resposta final da IA: o resumo do pensamento, os passos e o consumo do turno.
/// </summary>
public sealed record DetalhesDoTurno(string? Pensamento, IReadOnlyList<string> Passos, int? Tokens);
