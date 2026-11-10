using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DefinirMarkupDaVersao;

public sealed class DefinirMarkupDaVersaoCommandValidator : ValidatorBase<DefinirMarkupDaVersaoCommand>
{
    public DefinirMarkupDaVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        // Mesma faixa do markup do ateliê e do projeto.
        RuleFor(c => c.Markup)
            .GreaterThan(0)
            .LessThanOrEqualTo(ParametrosDePreco.MarkupMaximo)
            .PrecisionScale(5, ParametrosDePreco.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Markup")
            .When(c => c.Markup is not null);
    }
}
