using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Projetos de vários clientes numa consulta só (lista de clientes → projetos de cada um).
/// </summary>
public sealed class ProjetosPorClienteDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, Projeto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, Projeto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var projetos = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindBy(p => p.ClienteId != null && keys.Contains(p.ClienteId.Value))
            .OrderByDescending(p => p.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return projetos.ToLookup(p => p.ClienteId!.Value);
    }
}
