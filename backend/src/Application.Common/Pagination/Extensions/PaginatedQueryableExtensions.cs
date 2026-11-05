using Castwork.Application.Common.Pagination.Models;

namespace Castwork.Application.Common.Pagination.Extensions;

public static class PaginatedQueryableExtensions
{
    public static PaginatedQueryable<T> ToListaPaginada<T>(this IQueryable<T> source, QueryPaginada request)
        where T : class
        => new(source, request);
}
