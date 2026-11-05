using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

using FluentValidation;

namespace Castwork.Application.Acesso.Commands.RenovarSessao;

public sealed class RenovarSessaoCommandValidator : ValidatorBase<RenovarSessaoCommand>
{
    public RenovarSessaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(100).WithName("Refresh token");
}
