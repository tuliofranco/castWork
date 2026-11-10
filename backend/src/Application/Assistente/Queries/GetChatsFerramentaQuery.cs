using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// Os chats da conversa da ferramenta, do mais recente para o mais antigo (o em andamento incluído e marcado).
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetChatsFerramentaQuery : IRequest<IReadOnlyList<ChatDaConversaDto>>, IEscopoDaFerramenta
{
    public const int TituloMaxLength = 80;

    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetChatsFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetChatsFerramentaQuery, IReadOnlyList<ChatDaConversaDto>>
{
    public async Task<IReadOnlyList<ChatDaConversaDto>> Handle(GetChatsFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        if (await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken) is not { } conversa)
        {
            return [];
        }

        return conversa.Mensagens
            .GroupBy(m => m.Chat)
            .Select(chat =>
            {
                var visiveis = chat.Where(m => m.Papel != PapelMensagem.Ferramenta && (m.Texto is not null || m.Imagens.Count > 0)).OrderBy(m => m.Ordem).ToList();
                var primeira = visiveis.FirstOrDefault(m => m.Papel == PapelMensagem.Joalheiro);
                return new ChatDaConversaDto(
                    chat.Key,
                    chat.Min(m => m.CriadaEm),
                    chat.Max(m => m.CriadaEm),
                    Titulo(primeira?.Texto),
                    visiveis.Count,
                    chat.Key == conversa.ChatAtual);
            })
            .OrderByDescending(c => c.UltimaMensagemEm)
            .ThenByDescending(c => c.Numero)
            .ToList();
    }

    private static string? Titulo(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var linha = texto.ReplaceLineEndings(" ").Trim();
        return linha.Length > GetChatsFerramentaQuery.TituloMaxLength ? linha[..(GetChatsFerramentaQuery.TituloMaxLength - 1)] + "…" : linha;
    }
}
