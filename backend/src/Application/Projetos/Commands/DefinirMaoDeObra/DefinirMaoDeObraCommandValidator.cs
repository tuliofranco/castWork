using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DefinirMaoDeObra;

public sealed class DefinirMaoDeObraCommandValidator : ValidatorBase<DefinirMaoDeObraCommand>
{
    public DefinirMaoDeObraCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Tipo).IsInEnum().WithName("Tipo");

        RuleFor(c => c.Horas)
            .NotNull().WithMessage("Informe as horas.")
            .GreaterThan(0)
            .LessThanOrEqualTo(MaoDeObra.HorasMaximas)
            .PrecisionScale(6, MaoDeObra.HorasCasasDecimais, ignoreTrailingZeros: true)
            .WithName("Horas")
            .When(c => c.Tipo == TipoMaoDeObra.PorHoras);

        RuleFor(c => c.Valor)
            .NotNull().WithMessage("Informe o valor.")
            .GreaterThan(0)
            .LessThanOrEqualTo(MaoDeObra.ValorFixoMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Valor")
            .When(c => c.Tipo == TipoMaoDeObra.ValorFixo);
    }
}
