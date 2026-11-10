using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Mão de obra do orçamento da versão: por horas (horas × valor da hora do projeto) ou valor fixo.
/// </summary>
public sealed record MaoDeObra
{
    public const decimal HorasMaximas = 1_000m;
    public const decimal ValorFixoMaximo = 1_000_000m;
    public const int HorasCasasDecimais = 2;

    private MaoDeObra(TipoMaoDeObra tipo, decimal? horas, Dinheiro? valorFixo)
        => (Tipo, Horas, ValorFixo) = (tipo, horas, valorFixo);

    // Usado pelo EF ao ler do banco.
    private MaoDeObra()
    {
    }

    public TipoMaoDeObra Tipo { get; private set; }

    /// <summary>
    /// Horas de bancada, quando <see cref="Tipo"/> é <see cref="TipoMaoDeObra.PorHoras"/>.
    /// </summary>
    public decimal? Horas { get; private set; }

    /// <summary>
    /// Valor combinado, quando <see cref="Tipo"/> é <see cref="TipoMaoDeObra.ValorFixo"/>.
    /// </summary>
    public Dinheiro? ValorFixo { get; private set; }

    public static MaoDeObra PorHoras(decimal horas)
        => horas is > 0 and <= HorasMaximas
            ? new(TipoMaoDeObra.PorHoras, Math.Round(horas, HorasCasasDecimais, MidpointRounding.AwayFromZero), null)
            : throw new ValorInvalidoException(nameof(Horas), $"deve ser maior que zero e até {HorasMaximas:N0}");

    public static MaoDeObra Fixa(Dinheiro valor)
        => valor > Dinheiro.Zero && valor.Valor <= ValorFixoMaximo
            ? new(TipoMaoDeObra.ValorFixo, null, valor)
            : throw new ValorInvalidoException(nameof(ValorFixo), $"deve ser maior que zero e até {ValorFixoMaximo:N0}");

    /// <summary>
    /// Quanto custa, com o valor da hora do projeto (só usado quando é por horas).
    /// </summary>
    public Dinheiro Custo(Dinheiro valorHora)
        => Tipo == TipoMaoDeObra.PorHoras ? valorHora * (Horas ?? 0m) : ValorFixo ?? Dinheiro.Zero;
}
