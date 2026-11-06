using Castwork.Domain.Common;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// Uma mensagem da conversa da ferramenta. Como no briefing, <see cref="Texto"/> é o que aparece no chat e
/// <see cref="Conteudo"/> guarda a mensagem completa para retomar a conversa.
/// </summary>
public sealed class MensagemFerramenta : BaseEntity<long>
{
    internal MensagemFerramenta(int ordem, PapelMensagem papel, string? texto, string conteudo, IReadOnlyList<Guid> imagens, AcaoDaIa? acao, DateTimeOffset criadaEm)
    {
        Ordem = ordem;
        Papel = papel;
        Texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        Conteudo = string.IsNullOrWhiteSpace(conteudo) ? throw new ValorInvalidoException(nameof(Conteudo), "é obrigatório") : conteudo;
        Imagens = imagens.ToList();
        Acao = acao;
        CriadaEm = criadaEm;
    }

    private MensagemFerramenta()
    {
    }

    public int Ordem { get; private set; }

    public PapelMensagem Papel { get; private set; }

    public string? Texto { get; private set; }

    public string Conteudo { get; private set; } = string.Empty;

    /// <summary>
    /// Chaves das <see cref="ImagemConversa"/> da mensagem: anexadas pelo joalheiro ou geradas pela IA.
    /// </summary>
    public IReadOnlyList<Guid> Imagens { get; private set; } = [];

    /// <summary>
    /// O que a IA mudou no arquivo neste turno; nulo quando ela só conversou.
    /// </summary>
    public AcaoDaIa? Acao { get; private set; }

    public bool AcaoDesfeita { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    internal void MarcarDesfeita() => AcaoDesfeita = true;
}
