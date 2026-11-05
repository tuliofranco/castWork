using Castwork.Application.Common.Pagination.Models;

namespace Castwork.Application.Common.Pagination;

public sealed record ListaPaginada<T>(IReadOnlyList<T> Lista, int Pagina, int TamanhoPagina, int TotalRegistros)
{
    public int TotalPaginas => TotalRegistros == 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
}

public static class ListaPaginada
{
    public static ListaPaginada<T> Vazia<T>(QueryPaginada query) => new([], query.Pagina, query.TamanhoPagina, 0);
}
