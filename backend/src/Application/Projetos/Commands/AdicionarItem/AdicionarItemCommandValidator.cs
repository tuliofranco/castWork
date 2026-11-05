using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.AdicionarItem;

public sealed class AdicionarItemCommandValidator : ValidatorBase<AdicionarItemCommand>
{
    public AdicionarItemCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.PecaId).GreaterThan(0).WithMessage("Informe a peça.").MustExists<AdicionarItemCommand, Peca>(UnitOfWork).WithName("Peça");
        RuleFor(c => c.Quantidade).InclusiveBetween(1, ItemComposicao.QuantidadeMaxima).WithName("Quantidade");
        RuleFor(c => c.Observacao).MaximumLength(ItemComposicao.ObservacaoMaxLength).WithName("Observação");
    }
}
