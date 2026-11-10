using Castwork.Application.Projetos.Queries.GetPesoDaPeca;

using GreenDonut;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// O peso de cada peça do projeto pedida, pela query <see cref="GetPesoDaPecaQuery"/> (mede o modelo 3D da peça; a medição
/// fica guardada pelo arquivo). Num escopo próprio e em sequência, como o preço da versão.
/// </summary>
public sealed class PesoPorPecaDoProjetoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<long, PesoDaPecaDto>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<long, PesoDaPecaDto>> LoadBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var pesos = new Dictionary<long, PesoDaPecaDto>();
        foreach (var pecaDoProjetoId in keys)
        {
            pesos[pecaDoProjetoId] = await mediator.Send(new GetPesoDaPecaQuery { PecaDoProjetoId = pecaDoProjetoId }, cancellationToken);
        }

        return pesos;
    }
}
