using Castwork.Application.Abstractions;
using Castwork.Application.Pecas;
using Castwork.Domain.Entities.Pecas;
using Castwork.GraphQl.Types.Pecas;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

namespace Castwork.GraphQl.Queries;

/// <summary>
/// Leitura simples: devolve IQueryable e deixa paginação, filtro e ordenação virarem SQL.
/// <c>[Serial]</c>: resolvers que usam o DbContext da requisição não rodam em paralelo entre si.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class PecasQueries
{
    [Serial]
    [Authorize(Policy = Politicas.PecasLer)]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering(typeof(PecaFilterType))]
    [UseSorting(typeof(PecaSortType))]
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<PecaType>>>))]
    [GraphQLDescription("Peças do ateliê, paginadas. Sem ordenação informada, as mais recentes primeiro.")]
    public IQueryable<Peca> GetPecas(
        [GraphQLDescription("Trecho do título da peça ou do nome do cliente, sem diferenciar maiúsculas.")] string? termo,
        [Service] IUnitOfWork unitOfWork)
        => unitOfWork.GetRepository<Peca>().FindAll().ComTermo(termo).AsNoTracking().OrderByDescending(p => p.Id);

    [Serial]
    [Authorize(Policy = Politicas.PecasLer)]
    [GraphQLType(typeof(PecaType))]
    [GraphQLDescription("Uma peça pelo Id, ou null se não existir.")]
    public Task<Peca?> GetPeca(long id, [Service] IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<Peca>().FindBy(p => p.Id == id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
