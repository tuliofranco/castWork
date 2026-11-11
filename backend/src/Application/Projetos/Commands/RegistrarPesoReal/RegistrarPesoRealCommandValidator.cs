using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.RegistrarPesoReal;

public sealed class RegistrarPesoRealCommandValidator : ValidatorBase<RegistrarPesoRealCommand>
{
    public RegistrarPesoRealCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        // A liga precisa estar nos metais da versão (o agregado confere); pode estar desativada.
        RuleFor(c => c.LigaId).GreaterThan(0).WithName("Liga");
        RuleFor(c => c.PesoGramas)
            .GreaterThan(0)
            .LessThanOrEqualTo(MetalDaVersao.PesoMaximoGramas)
            .PrecisionScale(10, 3, ignoreTrailingZeros: true)
            .WithName("Peso real")
            .When(c => c.PesoGramas is not null);
    }
}
