using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

public sealed class CriarPecaCommandValidator : ValidatorBase<CriarPecaCommand>
{
    public CriarPecaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarDadosDaPeca();
}
