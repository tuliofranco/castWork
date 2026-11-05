using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Projetos.Commands.PublicarVersao;

public sealed class PublicarVersaoCommandValidator : ValidatorBase<PublicarVersaoCommand>
{
    public PublicarVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
