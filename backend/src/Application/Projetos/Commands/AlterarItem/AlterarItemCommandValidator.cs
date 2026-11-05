using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.AlterarItem;

public sealed class AlterarItemCommandValidator : ValidatorBase<AlterarItemCommand>
{
    public AlterarItemCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Quantidade).InclusiveBetween(1, ItemComposicao.QuantidadeMaxima).WithName("Quantidade");
        RuleFor(c => c.Observacao).MaximumLength(ItemComposicao.ObservacaoMaxLength).WithName("Observação");
    }
}
