using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Precificacao;

using FluentValidation;

namespace Castwork.Application.Precificacao.Commands.AlterarLiga;

public sealed class AlterarLigaCommandValidator : ValidatorBase<AlterarLigaCommand>
{
    public AlterarLigaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        // Só liga ativa: a desativada sai da tela e dos orçamentos novos.
        RuleFor(c => c.LigaId).GreaterThan(0).MustExists<AlterarLigaCommand, Liga>(UnitOfWork).WithName("Liga");
        this.ValidarDadosDaLiga(UnitOfWork, c => c.LigaId);
    }
}
