using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Acesso;

using FluentValidation;

namespace Castwork.Application.Acesso.Commands.CriarPerfil;

public sealed class CriarPerfilCommandValidator : ValidatorBase<CriarPerfilCommand>
{
    public CriarPerfilCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome)
            .NotEmpty()
            .MaximumLength(Perfil.NomeMaxLength)
            .MustAsync(SerNomeUnicoAsync).WithMessage("Já existe um perfil com este nome.")
            .WithName("Nome");

        RuleForEach(c => c.Regras)
            .Must(CatalogoDeRegras.Existe).WithMessage("Regra '{PropertyValue}' não existe.");
    }

    private async Task<bool> SerNomeUnicoAsync(string nome, CancellationToken cancellationToken)
        => !await UnitOfWork.GetRepository<Perfil>().FindBy(p => p.Nome == nome.Trim()).AnyAsync(cancellationToken);
}
