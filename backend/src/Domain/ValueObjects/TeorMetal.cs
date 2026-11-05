using System.Globalization;

using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// Teor (pureza) de uma liga em milésimos: 750 = ouro 18k, 925 = prata 925.
/// Milésimos são a unidade canônica porque valem para qualquer metal; quilates só fazem sentido para o ouro.
/// </summary>
public sealed record TeorMetal
{
    public const int MilesimosMaximo = 1000;

    private TeorMetal(int milesimos) => Milesimos = milesimos;

    public static TeorMetal Ouro24k { get; } = new(999);

    public static TeorMetal Ouro18k { get; } = new(750);

    public static TeorMetal Ouro14k { get; } = new(585);

    public static TeorMetal Ouro10k { get; } = new(417);

    public static TeorMetal Prata950 { get; } = new(950);

    public static TeorMetal Prata925 { get; } = new(925);

    public int Milesimos { get; }

    /// <summary>
    /// Fração de metal puro na liga (0,750 para 18k). É o fator usado na precificação.
    /// </summary>
    public decimal Pureza => Milesimos / (decimal)MilesimosMaximo;

    /// <summary>
    /// Equivalente em quilates, com uma casa (750 → 18,0; 585 → 14,0).
    /// </summary>
    public decimal Quilates => Math.Round(Pureza * 24m, 1, MidpointRounding.AwayFromZero);

    public static TeorMetal EmMilesimos(int milesimos)
        => milesimos is <= 0 or > MilesimosMaximo
            ? throw new ValorInvalidoException(nameof(TeorMetal), $"deve estar entre 1 e {MilesimosMaximo} milésimos")
            : new TeorMetal(milesimos);

    /// <summary>
    /// Aceita a notação usada no balcão: "18k", "18K", "750", "925".
    /// Quilates comerciais seguem a tabela usual (14k = 585, não 583).
    /// </summary>
    public static TeorMetal Parse(string texto)
    {
        var normalizado = texto.Trim().ToUpperInvariant();

        if (normalizado.EndsWith('K')
            && int.TryParse(normalizado[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out var quilates))
        {
            return quilates switch
            {
                24 => Ouro24k,
                18 => Ouro18k,
                14 => Ouro14k,
                10 => Ouro10k,
                > 0 and <= 24 => EmMilesimos((int)Math.Round(quilates / 24m * MilesimosMaximo, MidpointRounding.AwayFromZero)),
                _ => throw new ValorInvalidoException(nameof(TeorMetal), "em quilates deve estar entre 1k e 24k"),
            };
        }

        return int.TryParse(normalizado, NumberStyles.None, CultureInfo.InvariantCulture, out var milesimos)
            ? EmMilesimos(milesimos)
            : throw new ValorInvalidoException(nameof(TeorMetal), $"'{texto}' não é um teor válido (use 18k ou 750)");
    }

    public override string ToString() => Milesimos.ToString(CultureInfo.InvariantCulture);
}
