namespace Castwork.Application.Abstractions;

/// <summary>
/// Consulta se um perfil possui uma regra. Implementada pelo módulo Acesso (Etapa 6), com cache.
/// </summary>
public interface IPermissaoService
{
    Task<bool> PerfilPossuiRegraAsync(long perfilId, Guid regra, CancellationToken cancellationToken);

    /// <summary>
    /// Descarta o cache do perfil após mudar suas regras.
    /// </summary>
    void InvalidarCache(long perfilId);
}
