using Castwork.Application.Common.Pagination.Models;

namespace Castwork.Application.Common.Pagination.Extensions;

public static class QueryPaginadaExtensions
{
    /// <summary>
    /// Copia a paginação de uma query para outra (ex.: uma query que delega a outra).
    /// </summary>
    public static TDestino WithPaginationFrom<TDestino>(this TDestino destino, QueryPaginada origem)
        where TDestino : QueryPaginada
    {
        destino.Pagina = origem.Pagina;
        destino.TamanhoPagina = origem.TamanhoPagina;
        destino.OrdenarPor = origem.OrdenarPor;
        destino.Decrescente = origem.Decrescente;
        return destino;
    }
}
