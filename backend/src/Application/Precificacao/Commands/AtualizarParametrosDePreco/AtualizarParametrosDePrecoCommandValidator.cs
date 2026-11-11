using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;

public sealed class AtualizarParametrosDePrecoCommandValidator : ValidatorBase<AtualizarParametrosDePrecoCommand>
{
    public AtualizarParametrosDePrecoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarParametrosDePreco();
}
