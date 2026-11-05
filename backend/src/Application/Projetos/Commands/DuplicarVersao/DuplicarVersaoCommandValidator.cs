using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DuplicarVersao;

public sealed class DuplicarVersaoCommandValidator : ValidatorBase<DuplicarVersaoCommand>
{
    public DuplicarVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Nome).MaximumLength(VersaoProjeto.NomeMaxLength).WithName("Nome");
    }
}
