using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Precificacao.Commands;

public static class ParametrosDePrecoExtensions
{
    /// <summary>
    /// Perda e taxas de 0% a 50%, valor da hora e markup positivos, arredondamento até R$ 1.000.
    /// </summary>
    public static void ValidarParametrosDePreco<T>(this AbstractValidator<T> validator)
        where T : IParametrosDePrecoInformados
    {
        validator.RuleFor(c => c.PerdaPercentual)
            .InclusiveBetween(0, ParametrosDePreco.PercentualMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Perda");

        validator.RuleFor(c => c.ValorHora)
            .GreaterThan(0)
            .LessThanOrEqualTo(ParametrosDePreco.ValorHoraMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Valor da hora");

        validator.RuleFor(c => c.Markup)
            .GreaterThan(0)
            .LessThanOrEqualTo(ParametrosDePreco.MarkupMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Markup");

        validator.RuleFor(c => c.TaxasPercentual)
            .InclusiveBetween(0, ParametrosDePreco.PercentualMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Taxas");

        validator.RuleFor(c => c.ArredondarPara)
            .InclusiveBetween(0, ParametrosDePreco.ArredondamentoMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Arredondamento");
    }

    /// <summary>
    /// Converte os campos validados no value object do domínio.
    /// </summary>
    public static ParametrosDePreco ParaParametrosDePreco(this IParametrosDePrecoInformados dados)
        => ParametrosDePreco.Criar(
            dados.PerdaPercentual,
            Dinheiro.Criar(dados.ValorHora),
            dados.Markup,
            dados.TaxasPercentual,
            Dinheiro.Criar(dados.ArredondarPara));
}
