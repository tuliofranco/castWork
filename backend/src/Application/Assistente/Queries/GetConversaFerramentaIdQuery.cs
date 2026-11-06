using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// O Id da conversa da ferramenta na versão (nulo se ainda não começou), para acompanhar o turno ao vivo.
/// Passa pela mesma checagem de permissão e de versão das outras leituras da conversa.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetConversaFerramentaIdQuery : IRequest<long?>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetConversaFerramentaIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetConversaFerramentaIdQuery, long?>
{
    public async Task<long?> Handle(GetConversaFerramentaIdQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var versao = ConversaDaVersao.VersaoDaRota(request.VersaoId);
        return await unitOfWork.GetRepository<Domain.Entities.Assistente.ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == request.ProjetoId && c.VersaoId == versao && c.Ferramenta == request.Ferramenta)
            .Select(c => (long?)c.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
