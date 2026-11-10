using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Peças de vários projetos numa consulta só, por nome.
/// </summary>
public sealed class PecasPorProjetoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, PecaDoProjeto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, PecaDoProjeto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var pecas = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindBy(p => keys.Contains(p.Id))
            .SelectMany(p => p.Pecas.Select(peca => new { ProjetoId = p.Id, Peca = peca }))
            .OrderBy(x => x.Peca.Nome).ThenBy(x => x.Peca.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return pecas.ToLookup(x => x.ProjetoId, x => x.Peca);
    }
}
