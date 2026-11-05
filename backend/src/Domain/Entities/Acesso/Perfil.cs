using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Conjunto de regras atribuído a usuários (ex.: "Bancada", "Atendimento").
/// </summary>
public sealed partial class Perfil : BaseEntity<long>, IAggregateRoot, IAuditavel
{
    public const int NomeMaxLength = 100;

    private readonly List<PerfilRegra> _regras = [];

    private Perfil()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public IReadOnlyCollection<PerfilRegra> Regras => _regras.AsReadOnly();

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
