using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Uma mensagem da conversa da ferramenta. Como no briefing, <see cref="Texto"/> é o que aparece no chat e
/// <see cref="Conteudo"/> guarda a mensagem completa para retomar a conversa.
/// </summary>
public sealed class MensagemFerramenta : BaseEntity<long>
{
    internal MensagemFerramenta(int ordem, int chat, PapelMensagem papel, string? texto, string conteudo, IReadOnlyList<Guid> imagens, AcaoDaIa? acao, DateTimeOffset criadaEm)
    {
        Ordem = ordem;
        Chat = chat;
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

    /// <summary>
    /// O chat da conversa a que a mensagem pertence (ver <see cref="ConversaFerramenta.ChatAtual"/>).
    /// </summary>
    public int Chat { get; private set; } = 1;

    public PapelMensagem Papel { get; private set; }

    public string? Texto { get; private set; }

    public string Conteudo { get; private set; } = string.Empty;

    /// <summary>
    /// Chaves das <see cref="ImagemConversa"/> da mensagem: anexadas pelo joalheiro ou geradas pela IA.
    /// </summary>
    public IReadOnlyList<Guid> Imagens { get; private set; } = [];

    /// <summary>
    /// Arquivos do projeto anexados à fala (o DXF do desenho 2D, o PDF do briefing).
    /// </summary>
    public IReadOnlyList<AnexoDaMensagem> Anexos { get; private set; } = [];

    /// <summary>
    /// O que a IA mudou no arquivo neste turno; nulo quando ela só conversou.
    /// </summary>
    public AcaoDaIa? Acao { get; private set; }

    public bool AcaoDesfeita { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>
    /// Resumo do que a IA pensou no turno (mostrado recolhido sob a resposta).
    /// </summary>
    public string? Pensamento { get; private set; }

    /// <summary>
    /// O que a IA fez no turno, em passos legíveis ("Desenhando círculos", "Conferindo o desenho").
    /// </summary>
    public IReadOnlyList<string> Passos { get; private set; } = [];

    /// <summary>
    /// Tokens que o turno consumiu (entrada, saída e cache), para o joalheiro acompanhar o custo.
    /// </summary>
    public int? Tokens { get; private set; }

    internal void DefinirDetalhes(DetalhesDoTurno detalhes)
    {
        Pensamento = string.IsNullOrWhiteSpace(detalhes.Pensamento) ? null : detalhes.Pensamento.Trim();
        Passos = detalhes.Passos.ToList();
        Tokens = detalhes.Tokens;
    }

    internal void DefinirAnexos(IReadOnlyList<AnexoDaMensagem> anexos) => Anexos = anexos.ToList();

    internal void MarcarDesfeita() => AcaoDesfeita = true;

    internal void DefinirConteudo(string conteudo)
        => Conteudo = string.IsNullOrWhiteSpace(conteudo) ? throw new ValorInvalidoException(nameof(Conteudo), "é obrigatório") : conteudo;
}
