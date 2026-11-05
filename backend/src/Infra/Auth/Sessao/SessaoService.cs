using System.Globalization;
using System.Security.Claims;

using Castwork.Application.Abstractions;
using Castwork.Infra.Auth.Jwt;

using Microsoft.AspNetCore.Http;

namespace Castwork.Infra.Auth.Sessao;

/// <summary>
/// Usuário da requisição, lido só das claims do token já validado pelo JwtBearer.
/// Fora de uma requisição HTTP (jobs, inicialização) não há usuário.
/// </summary>
public sealed class SessaoService(IHttpContextAccessor httpContextAccessor) : ISessaoService
{
    public long? UsuarioId => Long(CastworkClaims.UsuarioId);

    public string? Nome => Usuario?.FindFirstValue(CastworkClaims.Nome);

    public long? PerfilId => Long(CastworkClaims.PerfilId);

    public bool IsAdmin => Usuario?.FindFirstValue(CastworkClaims.Admin) == "true";

    private ClaimsPrincipal? Usuario => httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    private long? Long(string claim)
        => long.TryParse(Usuario?.FindFirstValue(claim), NumberStyles.None, CultureInfo.InvariantCulture, out var valor) ? valor : null;
}
