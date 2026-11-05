using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Validators;

using FluentValidation;

namespace Castwork.Application.Acesso.Commands.Login;

public sealed class LoginCommandValidator : ValidatorBase<LoginCommand>
{
    public LoginCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Email).NotEmpty().MaximumLength(150).WithName("E-mail");

        // Só limita o tamanho: um corpo gigante custaria caro no hash. A política fica no cadastro.
        RuleFor(c => c.Senha).NotEmpty().MaximumLength(SenhaValidatorExtensions.TamanhoMaximo).WithName("Senha");
    }
}
