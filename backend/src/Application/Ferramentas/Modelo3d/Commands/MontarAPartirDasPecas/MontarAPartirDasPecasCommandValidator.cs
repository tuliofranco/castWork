using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Projetos.Commands;

namespace Castwork.Application.Ferramentas.Modelo3d.Commands.MontarAPartirDasPecas;

/// <summary>
/// A versão (e se está em desenvolvimento) o agregado confere; aqui, o projeto.
/// </summary>
public sealed class MontarAPartirDasPecasCommandValidator : ValidatorBase<MontarAPartirDasPecasCommand>
{
    public MontarAPartirDasPecasCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
