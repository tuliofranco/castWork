using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Definição do que será feito no projeto, acima das versões. Nasce do formulário inicial e é refinado
/// na conversa com a IA; um por projeto. A conversa pertence ao agregado para ser retomada depois.
/// </summary>
public sealed partial class Briefing : BaseEntity<long>, IAggregateRoot, IAuditavel
{
    public const int TextoCurtoMaxLength = 300;
    public const int TextoLongoMaxLength = 2000;

    private readonly List<MensagemBriefing> _mensagens = [];

    private Briefing()
    {
    }

    public long ProjetoId { get; private set; }

    public TipoJoia TipoJoia { get; private set; }

    /// <summary>
    /// O pedido como o joalheiro anotou no balcão (formulário inicial).
    /// </summary>
    public string OQueOClienteQuer { get; private set; } = string.Empty;

    public string? Ocasiao { get; private set; }

    public string? Materiais { get; private set; }

    public string? Medidas { get; private set; }

    public DateOnly? Prazo { get; private set; }

    /// <summary>
    /// Valor ou faixa, como o cliente falou ("até R$ 3.000").
    /// </summary>
    public string? Orcamento { get; private set; }

    /// <summary>
    /// A definição consolidada pela conversa: o que de fato será produzido.
    /// </summary>
    public string? OQueSeraFeito { get; private set; }

    public string? Acabamento { get; private set; }

    public string? Pedras { get; private set; }

    public string? Fecho { get; private set; }

    public string? Observacoes { get; private set; }

    public IReadOnlyCollection<MensagemBriefing> Mensagens => _mensagens.AsReadOnly();

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
