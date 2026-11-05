using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Pecas;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Peças de vários clientes numa consulta só (lista de clientes → histórico de cada um).
/// </summary>
public sealed class PecasPorClienteDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, Peca>(batchScheduler, options)
{
    protected override async Task<ILookup<long, Peca>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var pecas = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Peca>()
            .FindBy(p => keys.Contains(p.ClienteId))
            .OrderByDescending(p => p.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return pecas.ToLookup(p => p.ClienteId);
    }
}
