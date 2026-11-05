using MediatR;

namespace Castwork.Application.Common.Pagination.Models;

/// <summary>
/// Base das queries paginadas: já amarra o retorno do MediatR em <see cref="ListaPaginada{T}"/>.
/// </summary>
public abstract class QueryPaginadaPara<T> : QueryPaginada, IRequest<ListaPaginada<T>>;
