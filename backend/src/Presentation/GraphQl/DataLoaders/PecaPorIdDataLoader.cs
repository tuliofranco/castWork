using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Pecas;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Peças de todos os itens das composições pedidas, numa consulta só.
/// </summary>
public sealed class PecaPorIdDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, Peca>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, Peca>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Peca>()
            .FindBy(p => keys.Contains(p.Id))
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }
}
