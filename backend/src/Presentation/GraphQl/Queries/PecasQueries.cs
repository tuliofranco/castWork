using Castwork.Application.Abstractions;
using Castwork.Application.Pecas;
using Castwork.Domain.Entities.Pecas;
using Castwork.GraphQl.Types.Pecas;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

namespace Castwork.GraphQl.Queries;

/// <summary>
/// Biblioteca de peças. <c>[Serial]</c>: resolvers que usam o DbContext da requisição não rodam em paralelo.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class PecasQueries
{
    [Serial]
    [Authorize(Policy = Politicas.PecasLer)]
    [GraphQLDescription("Biblioteca de peças, paginada. Sem ordenação informada, por nome.")]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering(typeof(PecaFilterType))]
    [UseSorting(typeof(PecaSortType))]
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<PecaType>>>))]
    public IQueryable<Peca> GetPecas(
        [GraphQLDescription("Trecho do nome ou do material, sem diferenciar maiúsculas.")] string? termo,
        [Service] IUnitOfWork unitOfWork)
        => unitOfWork.GetRepository<Peca>().FindAll().ComTermo(termo).AsNoTracking().OrderBy(p => p.Nome).ThenBy(p => p.Id);

    [Serial]
    [Authorize(Policy = Politicas.PecasLer)]
    [GraphQLDescription("Uma peça pelo Id, ou null se não existir.")]
    [GraphQLType(typeof(PecaType))]
    public Task<Peca?> GetPeca(long id, [Service] IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<Peca>().FindBy(p => p.Id == id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
