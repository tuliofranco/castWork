using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Validators;

using FluentValidation;

namespace Castwork.Application.Acesso.Commands.AlterarMinhaSenha;

public sealed class AlterarMinhaSenhaCommandValidator : ValidatorBase<AlterarMinhaSenhaCommand>
{
    public AlterarMinhaSenhaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.SenhaAtual).NotEmpty().MaximumLength(SenhaValidatorExtensions.TamanhoMaximo).WithName("Senha atual");
        RuleFor(c => c.NovaSenha)
            .SenhaForte()
            .NotEqual(c => c.SenhaAtual).WithMessage("A nova senha deve ser diferente da atual.")
            .WithName("Nova senha");
    }
}
