using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Projetos.Commands.RemoverItem;

public sealed class RemoverItemCommandValidator : ValidatorBase<RemoverItemCommand>
{
    public RemoverItemCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
