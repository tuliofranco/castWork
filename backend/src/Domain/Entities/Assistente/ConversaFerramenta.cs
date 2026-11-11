using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// A conversa do joalheiro com a IA dentro de uma ferramenta (ex.: <c>desenho-2d</c>) de uma versão, de uma peça do
/// projeto ou do próprio projeto. Cada ferramenta de cada escopo tem a sua: a IA recebe o contexto e as ferramentas (MCP)
/// só daquela.
/// </summary>
public sealed partial class ConversaFerramenta : BaseEntity<long>, IAggregateRoot
{
    public const int FerramentaMaxLength = 40;

    /// <summary>
    /// Quanto um turno da IA pode durar; depois disso, uma fala sem resposta é considerada abandonada.
    /// </summary>
    public static readonly TimeSpan TempoMaximoDoTurno = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Conteúdo da fala enquanto a IA trabalha (a coluna é jsonb e obrigatória).
    /// </summary>
    private const string ConteudoProvisorio = "{\"blocos\":[]}";

    private readonly List<MensagemFerramenta> _mensagens = [];

    private ConversaFerramenta()
    {
    }

    public long ProjetoId { get; private set; }

    /// <summary>
    /// A versão da conversa; nula nas ferramentas do projeto (o briefing vale para todas as versões) e nas da peça.
    /// </summary>
    public long? VersaoId { get; private set; }

    /// <summary>
    /// A peça do projeto da conversa (o desenho 2D e o modelo 3D de cada peça são só dela); nula nas ferramentas do
    /// projeto e da versão.
    /// </summary>
    public long? PecaDoProjetoId { get; private set; }

    /// <summary>
    /// Código da ferramenta, como aparece na rota (<c>desenho-2d</c>).
    /// </summary>
    public string Ferramenta { get; private set; } = string.Empty;

    public IReadOnlyCollection<MensagemFerramenta> Mensagens => _mensagens.AsReadOnly();

    /// <summary>
    /// Número do chat em andamento. "Novo chat" passa para o próximo; os anteriores ficam no histórico e
    /// podem ser retomados. O arquivo (o desenho) é um só para todos os chats da ferramenta na versão.
    /// </summary>
    public int ChatAtual { get; private set; } = 1;

    /// <summary>
    /// As mensagens do chat em andamento: só elas vão para a IA.
    /// </summary>
    public IEnumerable<MensagemFerramenta> MensagensDoChat => _mensagens.Where(m => m.Chat == ChatAtual).OrderBy(m => m.Ordem);

    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>
    /// Só na conversa de uma peça do projeto: o arquivo do histórico que o joalheiro escolheu como o desenho da peça. É ele
    /// (e não o mais recente) que vale para o peso, a montagem na versão, a publicação, o visualizador e a ficha técnica.
    /// Nulo: a peça não tem desenho nesta ferramenta.
    /// </summary>
    public long? ArquivoFinalId { get; private set; }
}
