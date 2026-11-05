using System.Linq.Expressions;

using Castwork.Application.Common.Pagination.Models;

using Microsoft.EntityFrameworkCore;

namespace Castwork.Application.Common.Pagination;

/// <summary>
/// IQueryable "a caminho" de virar uma <see cref="ListaPaginada{T}"/>. Exige uma ordenação padrão
/// (<see cref="OrderBy{TKey}"/>/<see cref="OrderByDesc{TKey}"/>): sem ordem estável, páginas repetem ou pulam registros.
/// A ordenação pedida pelo front (<see cref="QueryPaginada.OrdenarPor"/>) vem primeiro e a padrão vira desempate.
/// </summary>
public sealed class PaginatedQueryable<T>
    where T : class
{
    private readonly IQueryable<T> _source;
    private readonly QueryPaginada _request;
    private readonly Func<IOrderedQueryable<T>, IOrderedQueryable<T>>? _desempate;
    private readonly Func<IQueryable<T>, IOrderedQueryable<T>>? _ordemPadrao;

    internal PaginatedQueryable(IQueryable<T> source, QueryPaginada request)
    {
        _source = source;
        _request = request;
    }

    private PaginatedQueryable(
        IQueryable<T> source,
        QueryPaginada request,
        Func<IQueryable<T>, IOrderedQueryable<T>> ordemPadrao,
        Func<IOrderedQueryable<T>, IOrderedQueryable<T>> desempate)
        : this(source, request)
    {
        _ordemPadrao = ordemPadrao;
        _desempate = desempate;
    }

    public PaginatedQueryable<T> OrderBy<TKey>(Expression<Func<T, TKey>> chave)
        => new(_source, _request, q => q.OrderBy(chave), q => q.ThenBy(chave));

    public PaginatedQueryable<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> chave)
        => new(_source, _request, q => q.OrderByDescending(chave), q => q.ThenByDescending(chave));

    /// <summary>
    /// Duas idas ao banco (contagem + página). A contagem só roda se a página veio cheia ou não é a primeira,
    /// o que poupa o COUNT nas listas curtas, o caso mais comum num ateliê.
    /// </summary>
    public async Task<ListaPaginada<T>> ToListAsync(CancellationToken cancellationToken)
    {
        if (_ordemPadrao is null || _desempate is null)
        {
            throw new InvalidOperationException("Não é permitido executar uma consulta paginada sem ordenação.");
        }

        var ordenada = string.IsNullOrWhiteSpace(_request.OrdenarPor)
            ? _ordemPadrao(_source)
            : _desempate(OrdenacaoDinamica.Ordenar(_source, _request.OrdenarPor, _request.Decrescente));

        var pagina = await ordenada
            .Skip((_request.Pagina - 1) * _request.TamanhoPagina)
            .Take(_request.TamanhoPagina)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var total = _request.Pagina == 1 && pagina.Count < _request.TamanhoPagina
            ? pagina.Count
            : await _source.CountAsync(cancellationToken);

        return new ListaPaginada<T>(pagina, _request.Pagina, _request.TamanhoPagina, total);
    }
}
