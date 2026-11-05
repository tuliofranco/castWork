using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Clientes;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Junta todos os Ids de cliente pedidos numa rodada de execução e busca de uma vez.
/// Usa um escopo próprio (e portanto um DbContext próprio): o DbContext não aceita consultas simultâneas
/// e o DataLoader pode rodar enquanto outro resolver usa o da requisição.
/// </summary>
public sealed class ClientePorIdDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, Cliente>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, Cliente>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Cliente>()
            .FindBy(c => keys.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }
}
