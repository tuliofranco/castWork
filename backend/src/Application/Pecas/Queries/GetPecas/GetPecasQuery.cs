using Castwork.Application.Abstractions;
using Castwork.Application.Common.Pagination;
using Castwork.Application.Common.Pagination.Extensions;
using Castwork.Application.Common.Pagination.Models;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using Mapster;

using MapsterMapper;

using MediatR;

namespace Castwork.Application.Pecas.Queries.GetPecas;

[Permissao(Regras.Pecas.Consultar)]
public sealed class GetPecasQuery : QueryPaginadaPara<GetPecasDto>
{
    /// <summary>
    /// Trecho do título da peça ou do nome do cliente.
    /// </summary>
    public string? Termo { get; set; }

    public StatusPeca? Status { get; set; }

    public long? ClienteId { get; set; }
}

public sealed class GetPecasQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetPecasQuery, ListaPaginada<GetPecasDto>>
{
    public async Task<ListaPaginada<GetPecasDto>> Handle(GetPecasQuery request, CancellationToken cancellationToken)
        => await unitOfWork.GetRepository<Peca>()
            .FindAll()
            .ComTermo(request.Termo)
            .ComStatus(request.Status)
            .DoCliente(request.ClienteId)
            .ProjectToType<GetPecasDto>(mapper.Config)
            .ToListaPaginada(request)
            .OrderByDesc(p => p.Id)
            .ToListAsync(cancellationToken);
}
