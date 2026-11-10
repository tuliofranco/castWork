using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;

public sealed class AtualizarParametrosDePrecoCommandValidator : ValidatorBase<AtualizarParametrosDePrecoCommand>
{
    public AtualizarParametrosDePrecoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.PerdaPercentual)
            .InclusiveBetween(0, ParametrosDePreco.PercentualMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Perda");

        RuleFor(c => c.ValorHora)
            .GreaterThan(0)
            .LessThanOrEqualTo(ParametrosDePreco.ValorHoraMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Valor da hora");

        RuleFor(c => c.Markup)
            .GreaterThan(0)
            .LessThanOrEqualTo(ParametrosDePreco.MarkupMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Markup");

        RuleFor(c => c.TaxasPercentual)
            .InclusiveBetween(0, ParametrosDePreco.PercentualMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Taxas");

        RuleFor(c => c.ArredondarPara)
            .InclusiveBetween(0, ParametrosDePreco.ArredondamentoMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Arredondamento");
    }
}
