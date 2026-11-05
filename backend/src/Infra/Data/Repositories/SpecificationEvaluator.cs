using Castwork.Domain.Specifications;

using Microsoft.EntityFrameworkCore;

namespace Castwork.Infra.Data.Repositories;

/// <summary>
/// Traduz a <see cref="BaseSpecification{T}"/> do domínio para IQueryable do EF.
/// </summary>
internal static class SpecificationEvaluator
{
    public static IQueryable<T> Aplicar<T>(IQueryable<T> query, BaseSpecification<T> specification)
        where T : class
    {
        if (specification.Criterio is not null)
        {
            query = query.Where(specification.Criterio);
        }

        query = specification.Includes.Aggregate(query, (atual, include) => atual.Include(include));

        if (specification.OrdenarPor is not null)
        {
            query = query.OrderBy(specification.OrdenarPor);
        }
        else if (specification.OrdenarPorDescendente is not null)
        {
            query = query.OrderByDescending(specification.OrdenarPorDescendente);
        }

        return query;
    }

    public static IQueryable<TResult> Aplicar<T, TResult>(IQueryable<T> query, BaseSpecification<T, TResult> specification)
        where T : class
        => Aplicar(query, (BaseSpecification<T>)specification).Select(specification.Projecao);
}
