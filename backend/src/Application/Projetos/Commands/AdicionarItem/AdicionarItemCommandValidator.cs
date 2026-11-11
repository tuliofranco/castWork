using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.AdicionarItem;

public sealed class AdicionarItemCommandValidator : ValidatorBase<AdicionarItemCommand>
{
    public AdicionarItemCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);

        // Só peça ativa do mesmo projeto: a de outro projeto não é encontrada.
        RuleFor(c => c.PecaDoProjetoId)
            .GreaterThan(0).WithMessage("Informe a peça.")
            .MustAsync((command, pecaId, cancellationToken) => UnitOfWork.GetRepository<Projeto>()
                .FindBy(p => p.Id == command.ProjetoId)
                .AnyAsync(p => p.Pecas.Any(x => x.Id == pecaId && x.Ativo), cancellationToken))
            .WithMessage("Peça não encontrada neste projeto.")
            .WithName("Peça");
        RuleFor(c => c.Quantidade).InclusiveBetween(1, ItemComposicao.QuantidadeMaxima).WithName("Quantidade");
        RuleFor(c => c.Observacao).MaximumLength(ItemComposicao.ObservacaoMaxLength).WithName("Observação");
    }
}
