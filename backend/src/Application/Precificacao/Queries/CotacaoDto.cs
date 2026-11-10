using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao.Queries;

/// <summary>
/// Uma linha do histórico de cotações.
/// </summary>
public sealed class CotacaoDto
{
    public long Id { get; init; }

    public Metal Metal { get; init; }

    /// <summary>
    /// R$/g do metal puro.
    /// </summary>
    public decimal Valor { get; init; }

    public DateTimeOffset RegistradaEm { get; init; }

    /// <summary>
    /// Nome de quem registrou; nulo se o usuário não existe mais.
    /// </summary>
    public string? RegistradaPor { get; init; }
}
