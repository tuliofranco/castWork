using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Uma mensagem da conversa do briefing. <see cref="Texto"/> é o que aparece no chat;
/// <see cref="Conteudo"/> guarda a mensagem completa (blocos da IA, ferramentas) para retomar a conversa.
/// </summary>
public sealed class MensagemBriefing : BaseEntity<long>
{
    internal MensagemBriefing(int ordem, PapelMensagem papel, string? texto, string conteudo, DateTimeOffset criadaEm)
    {
        Ordem = ordem;
        Papel = papel;
        Texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        Conteudo = string.IsNullOrWhiteSpace(conteudo) ? throw new ValorInvalidoException(nameof(Conteudo), "é obrigatório") : conteudo;
        CriadaEm = criadaEm;
    }

    private MensagemBriefing()
    {
    }

    public int Ordem { get; private set; }

    public PapelMensagem Papel { get; private set; }

    public string? Texto { get; private set; }

    public string Conteudo { get; private set; } = string.Empty;

    public DateTimeOffset CriadaEm { get; private set; }
}
