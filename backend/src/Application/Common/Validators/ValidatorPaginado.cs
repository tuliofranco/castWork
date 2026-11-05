using Castwork.Application.Abstractions;
using Castwork.Application.Common.Pagination;
using Castwork.Application.Common.Pagination.Models;

using FluentValidation;

namespace Castwork.Application.Common.Validators;

/// <summary>
/// Base dos validators de queries paginadas. O campo de ordenação precisa existir no DTO de retorno:
/// o DTO é a lista de campos que a tela pode ver e, portanto, ordenar.
/// </summary>
public abstract class ValidatorPaginado<TQuery, TDto> : ValidatorBase<TQuery>
    where TQuery : QueryPaginada
{
    protected ValidatorPaginado(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => RuleFor(q => q.OrdenarPor)
            .Must(OrdenacaoDinamica.EhCampoValido<TDto>)
            .WithMessage("Não é possível ordenar por '{PropertyValue}'.");
}
