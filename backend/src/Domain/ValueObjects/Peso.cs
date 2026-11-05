using System.Globalization;

using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// Peso em gramas com precisão de miligrama (3 casas), que é a precisão das balanças de joalheria.
/// </summary>
public sealed record Peso : IComparable<Peso>
{
    public const int CasasDecimais = 3;

    private Peso(decimal gramas) => Gramas = gramas;

    public static Peso Zero { get; } = new(0m);

    public decimal Gramas { get; }

    public static Peso operator +(Peso a, Peso b) => Somar(a, b);

    public static Peso operator *(Peso a, decimal fator) => Multiplicar(a, fator);

    public static bool operator >(Peso a, Peso b) => a.CompareTo(b) > 0;

    public static bool operator <(Peso a, Peso b) => a.CompareTo(b) < 0;

    public static bool operator >=(Peso a, Peso b) => a.CompareTo(b) >= 0;

    public static bool operator <=(Peso a, Peso b) => a.CompareTo(b) <= 0;

    public static Peso EmGramas(decimal gramas)
        => gramas < 0
            ? throw new ValorInvalidoException(nameof(Peso), "não pode ser negativo")
            : new Peso(Math.Round(gramas, CasasDecimais, MidpointRounding.AwayFromZero));

    public static Peso Somar(Peso a, Peso b) => EmGramas(a.Gramas + b.Gramas);

    public static Peso Multiplicar(Peso a, decimal fator) => EmGramas(a.Gramas * fator);

    public int CompareTo(Peso? other) => other is null ? 1 : Gramas.CompareTo(other.Gramas);

    public override string ToString() => $"{Gramas.ToString("0.000", CultureInfo.GetCultureInfo("pt-BR"))} g";
}
