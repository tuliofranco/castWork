using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Peças do projeto de todos os itens das composições pedidas, numa consulta só.
/// </summary>
public sealed class PecaDoProjetoPorIdDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, PecaDoProjeto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, PecaDoProjeto>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindAll()
            .SelectMany(p => p.Pecas)
            .Where(p => keys.Contains(p.Id))
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }
}
