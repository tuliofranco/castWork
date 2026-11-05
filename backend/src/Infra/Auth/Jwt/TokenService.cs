using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Acesso;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Castwork.Infra.Auth.Jwt;

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private const int BytesDoRefreshToken = 32;

    private readonly JwtOptions _options = options.Value;

    public TimeSpan ValidadeDoRefreshToken => TimeSpan.FromDays(_options.RefreshTokenDias);

    public TokenDeAcesso GerarAccessToken(Usuario usuario, IReadOnlyCollection<string> permissoes, string nomeDoAtelie)
    {
        var agora = timeProvider.GetUtcNow();
        var expiraEm = agora.AddMinutes(_options.AccessTokenMinutos);

        var claims = new List<Claim>
        {
            new(CastworkClaims.UsuarioId, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new(CastworkClaims.Nome, usuario.Nome),
            new(CastworkClaims.Email, usuario.Email),
            new(CastworkClaims.AtelieNome, nomeDoAtelie),
            new(CastworkClaims.Admin, usuario.IsAdmin ? "true" : "false"),
            new(CastworkClaims.VersaoSeguranca, usuario.VersaoSeguranca.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(permissoes.Select(p => new Claim(CastworkClaims.Permissoes, p)));

        if (usuario.PerfilId is long perfilId)
        {
            claims.Add(new Claim(CastworkClaims.PerfilId, perfilId.ToString(CultureInfo.InvariantCulture)));
        }

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _options.Emissor,
            Audience = _options.Audiencia,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(ParametrosDeValidacao.Chave(_options), ParametrosDeValidacao.Algoritmo),
        };

        var handler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false, MapInboundClaims = false };
        return new TokenDeAcesso(handler.CreateEncodedJwt(descritor), expiraEm);
    }

    public string GerarRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(BytesDoRefreshToken));

    public string CalcularHash(string refreshToken)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
