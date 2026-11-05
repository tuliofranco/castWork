using System.Globalization;

using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// Valor monetário em BRL, sempre com 2 casas e arredondamento comercial (meio para cima).
/// Não existe dinheiro negativo no domínio: descontos e diferenças são calculados antes de virar <see cref="Dinheiro"/>.
/// </summary>
public sealed record Dinheiro : IComparable<Dinheiro>
{
    public const int CasasDecimais = 2;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private Dinheiro(decimal valor) => Valor = valor;

    public static Dinheiro Zero { get; } = new(0m);

    public decimal Valor { get; }

    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => Somar(a, b);

    public static Dinheiro operator -(Dinheiro a, Dinheiro b) => Subtrair(a, b);

    public static Dinheiro operator *(Dinheiro a, decimal fator) => Multiplicar(a, fator);

    public static bool operator >(Dinheiro a, Dinheiro b) => a.CompareTo(b) > 0;

    public static bool operator <(Dinheiro a, Dinheiro b) => a.CompareTo(b) < 0;

    public static bool operator >=(Dinheiro a, Dinheiro b) => a.CompareTo(b) >= 0;

    public static bool operator <=(Dinheiro a, Dinheiro b) => a.CompareTo(b) <= 0;

    public static Dinheiro Criar(decimal valor)
        => valor < 0
            ? throw new ValorInvalidoException(nameof(Dinheiro), "não pode ser negativo")
            : new Dinheiro(Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero));

    public static Dinheiro Somar(Dinheiro a, Dinheiro b) => Criar(a.Valor + b.Valor);

    public static Dinheiro Subtrair(Dinheiro a, Dinheiro b) => Criar(a.Valor - b.Valor);

    public static Dinheiro Multiplicar(Dinheiro a, decimal fator) => Criar(a.Valor * fator);

    /// <summary>
    /// Acrescenta um percentual (ex.: margem de 30% → <c>AcrescentarPercentual(30)</c>).
    /// </summary>
    public Dinheiro AcrescentarPercentual(decimal percentual) => Multiplicar(this, 1 + (percentual / 100m));

    public int CompareTo(Dinheiro? other) => other is null ? 1 : Valor.CompareTo(other.Valor);

    public override string ToString() => Valor.ToString("C", PtBr);
}
