using Castwork.Application.Ferramentas.Modelo3d.Queries.GetConferenciaDoModelo;

using GreenDonut;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// Conferência de peso e materiais de cada versão pedida, pela query <see cref="GetConferenciaDoModeloQuery"/> (mede o
/// arquivo 3D atual; a medição fica guardada pelo arquivo). Num escopo próprio e em sequência, como o preço.
/// </summary>
public sealed class ConferenciaPorVersaoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, ConferenciaDoModeloDto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, ConferenciaDoModeloDto>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var conferencias = new Dictionary<long, ConferenciaDoModeloDto>();
        foreach (var versaoId in keys)
        {
            conferencias[versaoId] = await mediator.Send(new GetConferenciaDoModeloQuery { VersaoId = versaoId }, cancellationToken);
        }

        return conferencias;
    }
}
