using Castwork.Application.Abstractions;
using Castwork.Application.Pecas.Queries;
using Castwork.Domain.Entities.Pecas;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Os desenhos (2D e 3D) de várias peças da biblioteca numa consulta só, sem o conteúdo.
/// </summary>
public sealed class DesenhosPorPecaDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, DesenhoDaPecaDto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, DesenhoDaPecaDto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var desenhos = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Peca>()
            .FindBy(p => keys.Contains(p.Id))
            .SelectMany(p => p.Desenhos.Select(d => new DesenhoDaPecaDto { PecaId = p.Id, Ferramenta = d.Ferramenta, CopiadoEm = d.CopiadoEm, Tamanho = d.Conteudo.Length }))
            .OrderBy(d => d.Ferramenta)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return desenhos.ToLookup(d => d.PecaId);
    }
}
