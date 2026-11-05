using System.Collections.Concurrent;

using Castwork.Application.Abstractions;
using Castwork.Domain.Common;

namespace Castwork.Infra.Data.Repositories;

/// <summary>
/// Scoped por requisição. Os repositórios são criados sob demanda e reaproveitados dentro do mesmo escopo.
/// Repositórios específicos (quando uma consulta justificar) são registrados no DI e têm precedência.
/// </summary>
public sealed class UnitOfWork(ApplicationDbContext context, IServiceProvider serviceProvider) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositorios = new();

    public IRepository<T> GetRepository<T>()
        where T : BaseEntity<long>, IAggregateRoot
        => (IRepository<T>)_repositorios.GetOrAdd(
            typeof(T),
            _ => serviceProvider.GetService(typeof(IRepository<T>)) ?? new Repository<T>(context));

    public Task<int> SalvarAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
