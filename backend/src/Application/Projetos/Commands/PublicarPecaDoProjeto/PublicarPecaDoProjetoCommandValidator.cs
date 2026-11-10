using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

namespace Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;

/// <summary>
/// Os dados já passaram pelas mesmas validações de criar peça ao criar ou alterar a peça do projeto; o domínio confere
/// de novo ao criar a peça da biblioteca.
/// </summary>
public sealed class PublicarPecaDoProjetoCommandValidator : ValidatorBase<PublicarPecaDoProjetoCommand>
{
    public PublicarPecaDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
