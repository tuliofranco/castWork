using System.Linq.Expressions;

using Castwork.Domain.Common;
using Castwork.Domain.Specifications;

namespace Castwork.Application.Abstractions;

/// <summary>
/// Repositório genérico de agregados. Leituras devolvem <see cref="IQueryable{T}"/> para o handler compor
/// filtros e projetar (<c>ProjectTo</c>) antes de ir ao banco; nada é materializado aqui.
/// </summary>
public interface IRepository<T>
    where T : BaseEntity<long>, IAggregateRoot
{
    IQueryable<T> FindAll();

    IQueryable<T> FindBy(Expression<Func<T, bool>> criterio);

    IQueryable<T> FindBy(BaseSpecification<T> specification);

    IQueryable<TResult> FindBy<TResult>(BaseSpecification<T, TResult> specification);

    /// <summary>
    /// Carrega o agregado rastreado, para alteração. Retorna <c>null</c> se não existir.
    /// </summary>
    Task<T?> ObterPorIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// Como <see cref="ObterPorIdAsync"/>, mas lança <c>EntidadeNaoEncontradaException</c> (404) se não existir.
    /// </summary>
    Task<T> ObterPorIdOuFalharAsync(long id, CancellationToken cancellationToken);

    Task<bool> ExisteAsync(long id, CancellationToken cancellationToken);

    void Adicionar(T entidade);

    void Remover(T entidade);
}
