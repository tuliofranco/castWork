using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Itens de composição de várias versões numa consulta só, na ordem em que foram adicionados.
/// </summary>
public sealed class ItensPorVersaoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, ItemComposicao>(batchScheduler, options)
{
    protected override async Task<ILookup<long, ItemComposicao>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var itens = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindAll()
            .SelectMany(p => p.Versoes)
            .Where(v => keys.Contains(v.Id))
            .SelectMany(v => v.Itens.Select(i => new { VersaoId = v.Id, Item = i }))
            .OrderBy(x => x.Item.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return itens.ToLookup(x => x.VersaoId, x => x.Item);
    }
}
