using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Pecas;

using FluentValidation;

namespace Castwork.Application.Pecas.Commands.AlterarPeca;

public sealed class AlterarPecaCommandValidator : ValidatorBase<AlterarPecaCommand>
{
    public AlterarPecaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        // Só peça ativa: a desativada não entra em versão nova.
        RuleFor(c => c.PecaId).GreaterThan(0).MustExists<AlterarPecaCommand, Peca>(UnitOfWork).WithName("Peça");
        this.ValidarDadosDaPeca(UnitOfWork, soLigaAtiva: false);
    }
}
