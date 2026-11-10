using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;

/// <summary>
/// Padrões de preço do ateliê: cotação vigente de cada metal, ligas ativas e parâmetros.
/// </summary>
public sealed class PrecosDoAtelieDto
{
    /// <summary>
    /// Uma entrada por metal, na ordem do enum, mesmo sem cotação registrada.
    /// </summary>
    public IReadOnlyList<CotacaoVigenteDto> Cotacoes { get; init; } = [];

    public IReadOnlyList<LigaDto> Ligas { get; init; } = [];

    public ParametrosDePrecoDto Parametros { get; init; } = new();
}

public sealed class CotacaoVigenteDto
{
    public Metal Metal { get; init; }

    /// <summary>
    /// A mais recente; nula enquanto o ateliê não registrar nenhuma.
    /// </summary>
    public CotacaoDto? Atual { get; init; }
}

public sealed class LigaDto
{
    public long Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public Metal Metal { get; init; }

    /// <summary>
    /// Milésimos (750 = 18k).
    /// </summary>
    public int Teor { get; init; }

    /// <summary>
    /// g/cm³.
    /// </summary>
    public decimal Densidade { get; init; }
}

public sealed class ParametrosDePrecoDto
{
    public decimal PerdaPercentual { get; init; }

    /// <summary>
    /// Zero enquanto o ateliê não definir.
    /// </summary>
    public decimal ValorHora { get; init; }

    public decimal Markup { get; init; }

    public decimal TaxasPercentual { get; init; }

    public decimal ArredondarPara { get; init; }
}
