using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// A conversa da ferramenta na versão; vazia (sem mensagens nem arquivo) se ainda não começou.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetConversaFerramentaQuery : IRequest<ConversaFerramentaDto>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetConversaFerramentaQueryHandler(IUnitOfWork unitOfWork, TimeProvider relogio) : IRequestHandler<GetConversaFerramentaQuery, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(GetConversaFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return new ConversaFerramentaDto([], null, false, 1, 0);
        }

        var arquivoId = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversa.Id && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .Select(a => a.Conteudo.Length == 0 ? null : (long?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return Mapear(conversa, arquivoId, conversa.AguardandoIa(relogio.GetUtcNow()));
    }

    /// <summary>
    /// As mensagens visíveis de um chat (por padrão, o em andamento) e quantos outros chats a conversa tem.
    /// </summary>
    internal static ConversaFerramentaDto Mapear(ConversaFerramenta conversa, long? arquivoId, bool processando, int? chat = null) => new(
        conversa.Mensagens
            .Where(m => m.Chat == (chat ?? conversa.ChatAtual))
            .Where(m => m.Papel != PapelMensagem.Ferramenta && (m.Texto is not null || m.Imagens.Count > 0 || m.Anexos.Count > 0))
            .OrderBy(m => m.Ordem)
            .Select(m => new MensagemFerramentaDto(
                m.Ordem,
                m.Papel,
                m.Texto,
                m.Imagens,
                m.Acao is null ? null : new AcaoDaIaDto(m.Acao.Descricao, m.AcaoDesfeita),
                m.CriadaEm,
                m.Pensamento,
                m.Passos.Count > 0 ? m.Passos : null,
                m.Tokens,
                m.Anexos.Count > 0 ? m.Anexos.Select(a => new AnexoDaMensagemDto(a.Origem, a.Id, a.Nome)).ToList() : null))
            .ToList(),
        arquivoId,
        processando,
        chat ?? conversa.ChatAtual,
        conversa.Mensagens.Select(m => m.Chat).Distinct().Count(c => c != conversa.ChatAtual));
}
