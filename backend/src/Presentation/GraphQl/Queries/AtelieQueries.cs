using Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;

using HotChocolate.Authorization;

using MediatR;

namespace Castwork.GraphQl.Queries;

/// <summary>
/// Leitura com regra: delega ao MediatR, que aplica os mesmos behaviours (autorização, validação) do REST.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class AtelieQueries
{
    [Serial]
    [Authorize]
    [GraphQLDescription("Dados do ateliê da instalação (cabeçalho, documentos, valor/hora sugerido).")]
    public Task<ConfiguracaoAtelieDto> GetConfiguracaoAtelie([Service] ISender mediator, CancellationToken cancellationToken)
        => mediator.Send(new GetConfiguracaoAtelieQuery(), cancellationToken);
}
