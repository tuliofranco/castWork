namespace Castwork.Application.Precificacao.Commands;

/// <summary>
/// Os parâmetros de preço como chegam num command: os do ateliê (<c>AtualizarParametrosDePreco</c>) e os de um
/// projeto (<c>AlterarPrecosDoProjeto</c>). Mesmas faixas nos dois.
/// </summary>
public interface IParametrosDePrecoInformados
{
    /// <summary>
    /// % (8 = 8%).
    /// </summary>
    decimal PerdaPercentual { get; }

    decimal ValorHora { get; }

    /// <summary>
    /// Fator sobre o custo (1,8 = custo × 1,8).
    /// </summary>
    decimal Markup { get; }

    /// <summary>
    /// % de cartão e impostos.
    /// </summary>
    decimal TaxasPercentual { get; }

    /// <summary>
    /// Múltiplo em R$ para arredondar o preço para cima. Zero não arredonda.
    /// </summary>
    decimal ArredondarPara { get; }
}
