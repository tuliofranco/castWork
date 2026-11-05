using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Refresh token opaco. Só o hash SHA-256 é guardado: quem lê o banco não consegue usar os tokens.
/// Todos os tokens de uma mesma sessão compartilham a <see cref="Familia"/>: cada uso gera o próximo da família,
/// e reaproveitar um token já usado revoga a família inteira (sinal de que ele foi roubado).
/// </summary>
public sealed partial class RefreshToken : BaseEntity<long>, IAggregateRoot
{
    public const int TokenHashLength = 64;
    public const int MotivoMaxLength = 100;

    private RefreshToken()
    {
    }

    public long UsuarioId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid Familia { get; private set; }

    /// <summary>
    /// <see cref="Usuario.VersaoSeguranca"/> no momento da emissão. Se mudou, o token não vale mais.
    /// </summary>
    public Guid VersaoSegurancaDoUsuario { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset? UsadoEm { get; private set; }

    public DateTimeOffset? RevogadoEm { get; private set; }

    public string? MotivoRevogacao { get; private set; }

    public bool FoiUsadoOuRevogado => UsadoEm is not null || RevogadoEm is not null;
}
