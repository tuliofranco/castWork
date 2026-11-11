using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Pecas.Commands;

namespace Castwork.Application.Projetos.Commands.CriarPecaDoProjeto;

public sealed class CriarPecaDoProjetoCommandValidator : ValidatorBase<CriarPecaDoProjetoCommand>
{
    public CriarPecaDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        this.ValidarDadosDaPeca(UnitOfWork, soLigaAtiva: true);
    }
}
