using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Clientes;

/// <summary>
/// Cliente do ateliê. O histórico de peças não é uma coleção aqui: <c>Peca</c> é outro agregado e
/// referencia o cliente por <see cref="BaseEntity{TKey}.Id"/>. O histórico sai de uma consulta, sem carregar
/// todas as peças sempre que o cliente é alterado.
/// </summary>
public sealed partial class Cliente : BaseEntity<long>, IAggregateRoot, IAuditavel, IAtivavel
{
    public const int NomeMaxLength = 150;
    public const int EmailMaxLength = 150;
    public const int TelefoneMaxLength = 20;
    public const int ObservacoesMaxLength = 2000;

    private Cliente()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public Documento? Documento { get; private set; }

    public string? Email { get; private set; }

    public string? Telefone { get; private set; }

    public Endereco? Endereco { get; private set; }

    public string? Observacoes { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
