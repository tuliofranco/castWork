using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// As mensagens de um chat anterior, para leitura (sem o arquivo: o desenho é o mesmo para todos os chats).
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetChatFerramentaQuery : IRequest<ConversaFerramentaDto>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public int Numero { get; set; }
}

public sealed class GetChatFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetChatFerramentaQuery, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(GetChatFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null || !conversa.Mensagens.Any(m => m.Chat == request.Numero))
        {
            throw new EntidadeNaoEncontradaException("Chat", request.Numero);
        }

        return GetConversaFerramentaQueryHandler.Mapear(conversa, null, processando: false, request.Numero);
    }
}
