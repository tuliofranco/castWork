using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// A conversa do joalheiro com a IA dentro de uma ferramenta (ex.: <c>desenho-2d</c>) de uma versão.
/// Cada ferramenta da versão tem a sua: a IA recebe o contexto e as ferramentas (MCP) só daquela.
/// </summary>
public sealed partial class ConversaFerramenta : BaseEntity<long>, IAggregateRoot
{
    public const int FerramentaMaxLength = 40;

    private readonly List<MensagemFerramenta> _mensagens = [];

    private ConversaFerramenta()
    {
    }

    public long VersaoId { get; private set; }

    /// <summary>
    /// Código da ferramenta, como aparece na rota (<c>desenho-2d</c>).
    /// </summary>
    public string Ferramenta { get; private set; } = string.Empty;

    public IReadOnlyCollection<MensagemFerramenta> Mensagens => _mensagens.AsReadOnly();

    public DateTimeOffset CriadaEm { get; private set; }
}
