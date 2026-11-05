using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Projeto de uma arte (um colar, uma pulseira). Evolui em versões; cada versão tem a sua composição de peças.
/// Versões e itens pertencem ao agregado: a numeração sequencial e a regra "publicada não muda" são garantidas aqui.
/// </summary>
public sealed partial class Projeto : BaseEntity<long>, IAggregateRoot, IAuditavel
{
    public const int NomeMaxLength = 150;
    public const int DescricaoMaxLength = 2000;

    private readonly List<VersaoProjeto> _versoes = [];

    private Projeto()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public string? Descricao { get; private set; }

    /// <summary>
    /// Cliente para quem a arte é feita. Opcional: há projetos autorais, sem cliente.
    /// </summary>
    public long? ClienteId { get; private set; }

    public IReadOnlyCollection<VersaoProjeto> Versoes => _versoes.AsReadOnly();

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
