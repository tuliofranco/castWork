using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// O Id da conversa da ferramenta no escopo (projeto, versão ou peça) (nulo se ainda não começou), para acompanhar o turno ao vivo.
/// Passa pela mesma checagem de permissão e de versão das outras leituras da conversa.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetConversaFerramentaIdQuery : IRequest<long?>, IEscopoDaFerramenta
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetConversaFerramentaIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetConversaFerramentaIdQuery, long?>
{
    public async Task<long?> Handle(GetConversaFerramentaIdQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        return await ConversaDoEscopo.IdAsync(unitOfWork, request, request.Ferramenta, cancellationToken);
    }
}
