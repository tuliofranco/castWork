using Castwork.Application.Abstractions;
using Castwork.Application.Projetos;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.Types.Projetos;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

namespace Castwork.GraphQl.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ProjetosQueries
{
    [Serial]
    [Authorize(Policy = Politicas.ProjetosLer)]
    [GraphQLDescription("Projetos do ateliê, paginados. Sem ordenação informada, os mais recentes primeiro.")]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering(typeof(ProjetoFilterType))]
    [UseSorting(typeof(ProjetoSortType))]
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<ProjetoType>>>))]
    public IQueryable<Projeto> GetProjetos(
        [GraphQLDescription("Trecho do nome do projeto ou do cliente, sem diferenciar maiúsculas.")] string? termo,
        [Service] IUnitOfWork unitOfWork)
        => unitOfWork.GetRepository<Projeto>().FindAll()
            .ComTermo(termo, unitOfWork.GetRepository<Cliente>().FindAll())
            .AsNoTracking()
            .OrderByDescending(p => p.Id);

    [Serial]
    [Authorize(Policy = Politicas.ProjetosLer)]
    [GraphQLDescription("Um projeto pelo Id, ou null se não existir.")]
    [GraphQLType(typeof(ProjetoType))]
    public Task<Projeto?> GetProjeto(long id, [Service] IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<Projeto>().FindBy(p => p.Id == id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
