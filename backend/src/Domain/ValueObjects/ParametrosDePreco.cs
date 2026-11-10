using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// Parâmetros que entram em todo orçamento: perda de metal, valor da hora de bancada, markup, taxas e
/// arredondamento. O ateliê tem os seus padrões; o projeto copia e pode ajustar (etapa 3 da precificação).
/// </summary>
public sealed record ParametrosDePreco
{
    public const decimal PerdaPadrao = 8m;
    public const decimal MarkupPadrao = 1.8m;
    public const decimal TaxasPadrao = 0m;
    public const decimal ArredondamentoPadrao = 10m;

    public const decimal PercentualMaximo = 50m;
    public const decimal MarkupMaximo = 10m;
    public const decimal ValorHoraMaximo = 10_000m;
    public const decimal ArredondamentoMaximo = 1_000m;
    public const int CasasDecimais = 2;

    // Usado pelo EF ao ler do banco; no domínio, só pela fábrica.
    private ParametrosDePreco()
    {
    }

    /// <summary>
    /// % de metal que se perde entre o 3D e a peça pronta (canais, botão, polimento): o peso a fundir é o peso
    /// da peça acrescido dela.
    /// </summary>
    public decimal PerdaPercentual { get; private set; }

    public Dinheiro ValorHora { get; private set; } = Dinheiro.Zero;

    /// <summary>
    /// Fator sobre o custo (1,8 = custo × 1,8).
    /// </summary>
    public decimal Markup { get; private set; }

    /// <summary>
    /// % de cartão e impostos, somada sobre o preço.
    /// </summary>
    public decimal TaxasPercentual { get; private set; }

    /// <summary>
    /// O preço final sobe até o múltiplo deste valor (R$ 10 → R$ 1.234,00 vira R$ 1.240,00). Zero não arredonda.
    /// </summary>
    public Dinheiro ArredondarPara { get; private set; } = Dinheiro.Zero;

    public static ParametrosDePreco Criar(decimal perdaPercentual, Dinheiro valorHora, decimal markup, decimal taxasPercentual, Dinheiro arredondarPara)
        => new()
        {
            PerdaPercentual = Percentual(perdaPercentual, nameof(PerdaPercentual)),
            ValorHora = valorHora > Dinheiro.Zero && valorHora.Valor <= ValorHoraMaximo
                ? valorHora
                : throw new ValorInvalidoException(nameof(ValorHora), $"deve ser maior que zero e até {ValorHoraMaximo:N0}"),
            Markup = markup is > 0 and <= MarkupMaximo
                ? Math.Round(markup, CasasDecimais, MidpointRounding.AwayFromZero)
                : throw new ValorInvalidoException(nameof(Markup), $"deve ser maior que zero e até {MarkupMaximo}"),
            TaxasPercentual = Percentual(taxasPercentual, nameof(TaxasPercentual)),
            ArredondarPara = arredondarPara.Valor <= ArredondamentoMaximo
                ? arredondarPara
                : throw new ValorInvalidoException(nameof(ArredondarPara), $"deve ser até {ArredondamentoMaximo:N0}"),
        };

    /// <summary>
    /// Arredonda para cima, ao múltiplo de <see cref="ArredondarPara"/>.
    /// </summary>
    public Dinheiro Arredondar(Dinheiro preco)
        => ArredondarPara == Dinheiro.Zero
            ? preco
            : Dinheiro.Criar(Math.Ceiling(preco.Valor / ArredondarPara.Valor) * ArredondarPara.Valor);

    private static decimal Percentual(decimal valor, string campo)
        => valor is >= 0 and <= PercentualMaximo
            ? Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero)
            : throw new ValorInvalidoException(campo, $"deve estar entre 0% e {PercentualMaximo}%");
}
