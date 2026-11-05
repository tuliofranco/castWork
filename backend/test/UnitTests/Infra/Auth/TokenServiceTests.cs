using System.IdentityModel.Tokens.Jwt;

using Castwork.Domain.Entities.Acesso;
using Castwork.Infra.Auth.Jwt;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Moq;

namespace Castwork.UnitTests.Infra.Auth;

public class TokenServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Opcoes = new()
    {
        Emissor = "castwork-api",
        Audiencia = "castwork-app",
        ChaveAssinatura = new string('k', 48),
        AccessTokenMinutos = 15,
        RefreshTokenDias = 7,
    };

    [Test]
    public void DeveGerarAccessTokenAssinadoComClaimsDoUsuario()
    {
        var usuario = Usuario.Criar("Ana Lima", "ana@atelie.com", "hash", perfilId: 3, isAdmin: false);

        var token = Servico().GerarAccessToken(usuario, ["pecas.ler", "clientes.ler"], "Ateliê Lima");

        token.ExpiraEm.Should().Be(Agora.AddMinutes(15));
        var jwt = Validar(token.Token);
        jwt.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        jwt.Claims.Where(c => c.Type != CastworkClaims.Permissoes).ToDictionary(c => c.Type, c => c.Value).Should().Contain(new Dictionary<string, string>
        {
            [CastworkClaims.Nome] = "Ana Lima",
            [CastworkClaims.Email] = "ana@atelie.com",
            [CastworkClaims.AtelieNome] = "Ateliê Lima",
            [CastworkClaims.PerfilId] = "3",
            [CastworkClaims.Admin] = "false",
            [CastworkClaims.VersaoSeguranca] = usuario.VersaoSeguranca.ToString(),
            ["iss"] = "castwork-api",
            ["aud"] = "castwork-app",
        });
        jwt.Claims.Where(c => c.Type == CastworkClaims.Permissoes).Select(c => c.Value).Should().Equal("pecas.ler", "clientes.ler");
    }

    [Test]
    public void DeveOmitirPerfilNoTokenDeAdminSemPerfil()
    {
        var admin = Usuario.Criar("Admin", "admin@atelie.com", "hash", perfilId: null, isAdmin: true);

        var jwt = Validar(Servico().GerarAccessToken(admin, [], "Ateliê Lima").Token);

        jwt.Claims.Should().NotContain(c => c.Type == CastworkClaims.PerfilId);
        jwt.Claims.Should().Contain(c => c.Type == CastworkClaims.Admin && c.Value == "true");
    }

    [Test]
    public void DeveGerarRefreshTokensAleatoriosComHashDeterministicoDe64Caracteres()
    {
        var servico = Servico();

        var tokens = Enumerable.Range(0, 100).Select(_ => servico.GerarRefreshToken()).ToList();

        tokens.Should().OnlyHaveUniqueItems().And.AllSatisfy(t => t.Length.Should().Be(43, "256 bits em base64url"));
        servico.CalcularHash(tokens[0]).Should().MatchRegex("^[0-9a-f]{64}$").And.Be(servico.CalcularHash(tokens[0]));
    }

    [TestCase("")]
    [TestCase("chave-curta-demais")]
    public void DeveRejeitarChaveDeAssinaturaCurta(string chave)
    {
        var opcoes = new JwtOptions { Emissor = "e", Audiencia = "a", ChaveAssinatura = chave };

        var erros = opcoes.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(opcoes));

        erros.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(JwtOptions.ChaveAssinatura));
    }

    private static TokenService Servico()
    {
        var relogio = new Mock<TimeProvider>();
        relogio.Setup(r => r.GetUtcNow()).Returns(Agora);
        return new TokenService(Options.Create(Opcoes), relogio.Object);
    }

    private static JwtSecurityToken Validar(string token)
    {
        var parametros = ParametrosDeValidacao.Criar(Opcoes);
        parametros.ValidateLifetime = false;
        new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(token, parametros, out var validado);
        return (JwtSecurityToken)validado;
    }
}
