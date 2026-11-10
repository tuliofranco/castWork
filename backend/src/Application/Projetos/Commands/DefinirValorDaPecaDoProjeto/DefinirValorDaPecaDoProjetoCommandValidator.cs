using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Application.Pecas.Commands;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;

public sealed class DefinirValorDaPecaDoProjetoCommandValidator : ValidatorBase<DefinirValorDaPecaDoProjetoCommand>
{
    public DefinirValorDaPecaDoProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Valor).ValorDaPeca().When(c => c.Valor is not null);
    }
}
