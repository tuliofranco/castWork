using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Versões de vários projetos numa consulta só. Versão não tem repositório próprio (é filha do agregado),
/// então a consulta parte dos projetos.
/// </summary>
public sealed class VersoesPorProjetoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, VersaoProjeto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, VersaoProjeto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var versoes = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindBy(p => keys.Contains(p.Id))
            .SelectMany(p => p.Versoes.Select(v => new { ProjetoId = p.Id, Versao = v }))
            .OrderByDescending(x => x.Versao.Numero)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return versoes.ToLookup(x => x.ProjetoId, x => x.Versao);
    }
}
