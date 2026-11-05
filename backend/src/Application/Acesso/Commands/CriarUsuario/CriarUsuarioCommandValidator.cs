using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Acesso;

using FluentValidation;

namespace Castwork.Application.Acesso.Commands.CriarUsuario;

public sealed class CriarUsuarioCommandValidator : ValidatorBase<CriarUsuarioCommand>
{
    public CriarUsuarioCommandValidator(IUnitOfWork unitOfWork, ISessaoService sessao)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(Usuario.NomeMaxLength).WithName("Nome");

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(Usuario.EmailMaxLength)
            .MustAsync(SerEmailUnicoAsync).WithMessage("Já existe um usuário com este e-mail.")
            .WithName("E-mail");

        RuleFor(c => c.Senha).SenhaForte().WithName("Senha");

        RuleFor(c => c.PerfilId)
            .NotNull().WithMessage("Informe o perfil.")
            .MustExists<CriarUsuarioCommand, Perfil>(UnitOfWork)
            .When(c => !c.IsAdmin)
            .WithName("Perfil");

        // A regra GerenciarUsuarios permite criar usuários, mas só um admin cria outro admin.
        RuleFor(c => c.IsAdmin)
            .Must(isAdmin => !isAdmin || sessao.IsAdmin)
            .WithMessage("Somente administradores podem criar outro administrador.");
    }

    private async Task<bool> SerEmailUnicoAsync(string email, CancellationToken cancellationToken)
        => !await UnitOfWork.GetRepository<Usuario>()
            .FindBy(u => u.Email == Usuario.NormalizarEmail(email))
            .AnyAsync(cancellationToken);
}
