using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Pecas.Commands;

namespace Castwork.Application.Projetos.Commands.AlterarPecaDoProjeto;

public sealed class AlterarPecaDoProjetoCommandValidator : ValidatorBase<AlterarPecaDoProjetoCommand>
{
    public AlterarPecaDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        this.ValidarDadosDaPeca(UnitOfWork, soLigaAtiva: false);
    }
}
