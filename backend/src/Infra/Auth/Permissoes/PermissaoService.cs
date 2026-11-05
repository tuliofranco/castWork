using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Castwork.Infra.Auth.Permissoes;

/// <summary>
/// Regras de cada perfil em cache de memória por 5 minutos: o AuthorizationBehaviour roda em todo request
/// e não deve ir ao banco toda vez. Mudanças no perfil chamam <see cref="InvalidarCache"/>.
/// Em várias instâncias da API, a invalidação só vale para a instância local (as outras expiram em até 5 min).
/// </summary>
public sealed class PermissaoService(IUnitOfWork unitOfWork, IMemoryCache cache) : IPermissaoService
{
    private static readonly TimeSpan Expiracao = TimeSpan.FromMinutes(5);

    public async Task<bool> PerfilPossuiRegraAsync(long perfilId, Guid regra, CancellationToken cancellationToken)
    {
        var regras = await cache.GetOrCreateAsync(
            Chave(perfilId),
            entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = Expiracao;
                return CarregarAsync(perfilId, cancellationToken);
            });

        return regras!.Contains(regra);
    }

    public void InvalidarCache(long perfilId) => cache.Remove(Chave(perfilId));

    private static string Chave(long perfilId) => $"permissoes:perfil:{perfilId}";

    private async Task<HashSet<Guid>> CarregarAsync(long perfilId, CancellationToken cancellationToken)
        => [.. await unitOfWork.GetRepository<Perfil>()
            .FindBy(p => p.Id == perfilId)
            .SelectMany(p => p.Regras.Select(r => r.RegraId))
            .ToListAsync(cancellationToken)];
}
