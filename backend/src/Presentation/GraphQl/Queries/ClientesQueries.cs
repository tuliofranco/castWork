using Castwork.Application.Abstractions;
using Castwork.Application.Clientes;
using Castwork.Domain.Entities.Clientes;
using Castwork.GraphQl.Types.Clientes;

using HotChocolate.Authorization;

using Microsoft.EntityFrameworkCore;

namespace Castwork.GraphQl.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ClientesQueries
{
    [Serial]
    [Authorize(Policy = Politicas.ClientesLer)]
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering(typeof(ClienteFilterType))]
    [UseSorting(typeof(ClienteSortType))]
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<ClienteType>>>))]
    [GraphQLDescription("Clientes do ateliê, paginados. Sem ordenação informada, por nome.")]
    public IQueryable<Cliente> GetClientes(
        [GraphQLDescription("Trecho do nome, sem diferenciar maiúsculas.")] string? termo,
        [Service] IUnitOfWork unitOfWork)
        => unitOfWork.GetRepository<Cliente>().FindAll().ComNome(termo).AsNoTracking().OrderBy(c => c.Nome).ThenBy(c => c.Id);

    [Serial]
    [Authorize(Policy = Politicas.ClientesLer)]
    [GraphQLType(typeof(ClienteType))]
    [GraphQLDescription("Um cliente pelo Id, ou null se não existir.")]
    public Task<Cliente?> GetCliente(long id, [Service] IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<Cliente>().FindBy(c => c.Id == id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
