using Castwork.Application.Abstractions;
using Castwork.Application.Precificacao;
using Castwork.Application.Precificacao.Queries;
using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Entities.Precificacao;

using HotChocolate.Authorization;

using MediatR;

namespace Castwork.GraphQl.Queries;

/// <summary>
/// Preços do ateliê (cotações, ligas e parâmetros). A escrita é pelo REST (<c>api/precificacao</c>).
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class PrecificacaoQueries
{
    [Serial]
    [Authorize(Policy = Politicas.PrecificacaoLer)]
    [GraphQLDescription("Padrões de preço do ateliê: cotação vigente de cada metal (puro), ligas ativas e parâmetros.")]
    public Task<PrecosDoAtelieDto> GetPrecosDoAtelie([Service] ISender mediator, CancellationToken cancellationToken)
        => mediator.Send(new GetPrecosDoAtelieQuery(), cancellationToken);

    [Serial]
    [Authorize(Policy = Politicas.PrecificacaoLer)]
    [GraphQLDescription("Histórico de cotações de um metal, da mais recente para a mais antiga, paginado.")]
    [UsePaging(IncludeTotalCount = true)]
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<ObjectType<CotacaoDto>>>>))]
    public IQueryable<CotacaoDto> GetHistoricoDeCotacoes(Metal metal, [Service] IUnitOfWork unitOfWork)
        => unitOfWork.GetRepository<CotacaoMetal>()
            .FindBy(c => c.Metal == metal)
            .MaisRecentesPrimeiro()
            .ParaDto(unitOfWork.GetRepository<Usuario>().FindAll());
}
