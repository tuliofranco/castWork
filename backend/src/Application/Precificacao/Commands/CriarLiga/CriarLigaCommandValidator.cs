using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Precificacao.Commands.CriarLiga;

public sealed class CriarLigaCommandValidator : ValidatorBase<CriarLigaCommand>
{
    public CriarLigaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarDadosDaLiga(UnitOfWork, _ => 0);
}
