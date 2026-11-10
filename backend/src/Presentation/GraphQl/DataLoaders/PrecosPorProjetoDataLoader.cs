using Castwork.Application.Abstractions;
using Castwork.Application.Projetos;
using Castwork.Application.Projetos.Queries;
using Castwork.Domain.Entities.Projetos;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Preços de vários projetos numa consulta só (parâmetros nas colunas do projeto, cotações em <c>projeto_cotacoes</c>).
/// </summary>
public sealed class PrecosPorProjetoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, PrecosDoProjetoDto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, PrecosDoProjetoDto>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<Projeto>()
            .FindBy(p => keys.Contains(p.Id))
            .ParaPrecosDto()
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ProjetoId, cancellationToken);
    }
}
