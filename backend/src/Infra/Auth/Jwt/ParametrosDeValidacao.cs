using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Castwork.Infra.Auth.Jwt;

public static class ParametrosDeValidacao
{
    public const string Algoritmo = SecurityAlgorithms.HmacSha256;

    public static SymmetricSecurityKey Chave(JwtOptions options) => new(Encoding.UTF8.GetBytes(options.ChaveAssinatura));

    /// <summary>
    /// Validação completa: emissor, audiência, assinatura, expiração, algoritmo fixo (bloqueia "alg: none"
    /// e troca de algoritmo) e tolerância de relógio de só 30 segundos.
    /// </summary>
    public static TokenValidationParameters Criar(JwtOptions options) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Emissor,
        ValidateAudience = true,
        ValidAudience = options.Audiencia,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = Chave(options),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = [Algoritmo],
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = CastworkClaims.Nome,
    };
}
