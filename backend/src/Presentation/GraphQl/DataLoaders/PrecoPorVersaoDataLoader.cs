using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;

using GreenDonut;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Preço calculado de cada versão pedida, pela query <see cref="GetPrecoDaVersaoQuery"/> (com as permissões do usuário).
/// Num escopo próprio e em sequência: as consultas não disputam o DbContext com os outros resolvers.
/// </summary>
public sealed class PrecoPorVersaoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, PrecoDaVersaoDto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, PrecoDaVersaoDto>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var precos = new Dictionary<long, PrecoDaVersaoDto>();
        foreach (var versaoId in keys)
        {
            precos[versaoId] = await mediator.Send(new GetPrecoDaVersaoQuery { VersaoId = versaoId }, cancellationToken);
        }

        return precos;
    }
}
