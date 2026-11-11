using Castwork.Application.Abstractions;
using Castwork.Application.Pecas;
using Castwork.Application.Pecas.Queries;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Histórico de uso de várias peças da biblioteca numa consulta só, dos projetos mais recentes para os mais antigos.
/// </summary>
public sealed class HistoricoDeUsoPorPecaDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, UsoDaPecaDto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, UsoDaPecaDto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var usos = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindAll()
            .ParaHistoricoDeUso(keys)
            .OrderByDescending(u => u.ProjetoId).ThenBy(u => u.PecaDoProjetoId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return usos.ToLookup(u => u.PecaId);
    }
}
