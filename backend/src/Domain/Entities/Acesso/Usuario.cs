using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Acesso;

/// <summary>
/// Usuário do ateliê. O e-mail é o login. A senha nunca chega ao domínio: só o hash, gerado na Infra.
/// <see cref="VersaoSeguranca"/> muda quando algo sensível muda (senha, perfil, desativação) e invalida
/// os refresh tokens emitidos antes.
/// </summary>
public sealed partial class Usuario : BaseEntity<long>, IAggregateRoot, IAuditavel, IAtivavel
{
    public const int NomeMaxLength = 150;
    public const int EmailMaxLength = 150;
    public const int SenhaHashMaxLength = 200;
    public const int MaximoDeTentativas = 5;

    public static readonly TimeSpan DuracaoDoBloqueio = TimeSpan.FromMinutes(15);

    private Usuario()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string SenhaHash { get; private set; } = string.Empty;

    /// <summary>
    /// Perfil com as regras do usuário. Nulo só para administrador, que tem todas as regras.
    /// </summary>
    public long? PerfilId { get; private set; }

    public bool IsAdmin { get; private set; }

    public bool Ativo { get; private set; }

    public int TentativasFalhas { get; private set; }

    public DateTimeOffset? BloqueadoAte { get; private set; }

    public DateTimeOffset? UltimoLoginEm { get; private set; }

    public Guid VersaoSeguranca { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
