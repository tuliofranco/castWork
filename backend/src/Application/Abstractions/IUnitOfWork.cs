using Castwork.Domain.Common;

namespace Castwork.Application.Abstractions;

/// <summary>
/// Unidade de trabalho: um <c>SalvarAsync</c> por command, numa única transação do EF.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>()
        where T : BaseEntity<long>, IAggregateRoot;

    Task<int> SalvarAsync(CancellationToken cancellationToken);
}
