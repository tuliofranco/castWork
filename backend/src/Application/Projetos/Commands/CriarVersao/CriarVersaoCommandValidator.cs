using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.CriarVersao;

public sealed class CriarVersaoCommandValidator : ValidatorBase<CriarVersaoCommand>
{
    public CriarVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(VersaoProjeto.NomeMaxLength).WithName("Nome");
        RuleFor(c => c.Observacoes).MaximumLength(VersaoProjeto.ObservacoesMaxLength).WithName("Observações");
    }
}
