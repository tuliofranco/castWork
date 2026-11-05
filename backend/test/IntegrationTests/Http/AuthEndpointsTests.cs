using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Infra.Auth.Jwt;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.IntegrationTests.Http;

/// <summary>
/// Contrato HTTP de autenticação consumido pelo front Angular (data/auth/*).
/// </summary>
public class AuthEndpointsTests : TesteDeIntegracao
{
    private HttpClient _http = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar, Regras.Portfolio.Consultar).BuildAsync();
        await Usar(new UsuarioBuilder()).DoPerfil(perfil).CustomizeCommand(c => (c.Nome, c.Email) = ("Caio Aprendiz", "caio@atelie.com")).BuildAsync();
    }

    [TearDown]
    public void DisposeHttp() => _http.Dispose();

    [Test]
    public async Task DeveEntrarPorHttpComClaimsQueOFrontLeAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/auth/login", new { email = "caio@atelie.com", senha = UsuarioBuilder.SenhaPadrao });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessao = await resposta.Content.ReadFromJsonAsync<SessaoDto>();
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(sessao!.AccessToken).Claims.ToList();
        claims.Should().Contain(c => c.Type == CastworkClaims.Nome && c.Value == "Caio Aprendiz")
            .And.Contain(c => c.Type == CastworkClaims.Email && c.Value == "caio@atelie.com")
            .And.Contain(c => c.Type == CastworkClaims.AtelieNome && c.Value == "Meu ateliê");
        claims.Where(c => c.Type == CastworkClaims.Permissoes).Select(c => c.Value).Should().BeEquivalentTo("pecas.ler", "portfolio.ler");
        sessao.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task DeveResponder401EmProblemDetailsComMensagemGenericaAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/auth/login", new { email = "caio@atelie.com", senha = "senha-errada-1" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema.Should().BeEquivalentTo(new { Status = 401, Detail = "Usuário ou senha inválidos." });
        problema!.Extensions.Should().ContainKey("traceId");
    }

    [Test]
    public async Task DeveResponder400ComErrosPorCampoEmCamelCaseAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/auth/login", new { email = string.Empty, senha = string.Empty });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problema!.Errors.Keys.Should().BeEquivalentTo("email", "senha");
    }

    [Test]
    public async Task DeveRenovarEEncerrarSessaoPorHttpAsync()
    {
        var login = await (await _http.PostAsJsonAsync("/api/auth/login", new { email = "caio@atelie.com", senha = UsuarioBuilder.SenhaPadrao }))
            .Content.ReadFromJsonAsync<SessaoDto>();

        var renovada = await _http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login!.RefreshToken });
        var novo = await renovada.Content.ReadFromJsonAsync<SessaoDto>();
        var logout = await _http.PostAsJsonAsync("/api/auth/logout", new { refreshToken = novo!.RefreshToken });
        var depoisDoLogout = await _http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = novo.RefreshToken });

        (renovada.StatusCode, logout.StatusCode, depoisDoLogout.StatusCode)
            .Should().Be((HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeveEnviarHeadersDeSegurancaSemExporOServidorAsync()
    {
        var resposta = await _http.GetAsync(new Uri("/health", UriKind.Relative));

        resposta.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        resposta.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        resposta.Headers.Contains("Server").Should().BeFalse();
    }

    [Test]
    public async Task DeveLimitarTentativasDeLoginPorMinutoComRetryAfterAsync()
    {
        await using var factory = AmbienteDeTeste.CriarFactory(new Dictionary<string, string> { ["LimitesDeRequisicao:AutenticacaoPorMinuto"] = "3" });
        using var http = factory.CreateClient();
        var corpo = new { email = "caio@atelie.com", senha = "senha-errada-1" };

        var status = new List<HttpStatusCode>();
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 4; i++)
        {
            ultima = await http.PostAsJsonAsync("/api/auth/login", corpo);
            status.Add(ultima.StatusCode);
        }

        status.Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests);
        ultima!.Headers.RetryAfter.Should().NotBeNull();
    }
}
