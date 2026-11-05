using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Infra.Auth.Jwt;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Castwork.IntegrationTests.Http;

/// <summary>
/// Validação do JWT pelo pipeline HTTP real. O JwtBearer valida a expiração com o relógio real,
/// então os tokens são emitidos com o relógio de teste alinhado a "agora".
/// </summary>
public class JwtTests : TesteDeIntegracao
{
    private HttpClient _http = null!;
    private long _usuarioId;

    [SetUp]
    public void SetUp()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
    }

    [TearDown]
    public void DisposeHttp() => _http.Dispose();

    [Test]
    public async Task DeveLiberarHealthSemTokenEBarrarEndpointProtegidoAsync()
    {
        var health = await _http.GetAsync(new Uri("/health", UriKind.Relative));
        var protegido = await _http.GetAsync(new Uri("/api/auth/eu", UriKind.Relative));

        (health.StatusCode, protegido.StatusCode).Should().Be((HttpStatusCode.OK, HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task DeveAceitarTokenEmitidoNoLoginEExporClaimsAsync()
    {
        var token = await EntrarAsync();

        var resposta = await GetEuAsync(token);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<EuDto>()).Should().BeEquivalentTo(new EuDto(_usuarioId, "Ana Lima", false));
    }

    [Test]
    public async Task DeveRejeitarTokenExpiradoAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(-1);
        var token = await EntrarAsync();

        var resposta = await GetEuAsync(token);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeveRejeitarTokenComAssinaturaAdulteradaAsync()
    {
        var token = await EntrarAsync();
        var partes = token.Split('.');
        var payloadAdulterado = Base64UrlEncoder.Encode(Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(partes[1])).Replace("\"false\"", "\"true\"", StringComparison.Ordinal));

        var resposta = await GetEuAsync($"{partes[0]}.{payloadAdulterado}.{partes[2]}");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "virar admin editando o payload invalida a assinatura");
    }

    [Test]
    public async Task DeveRejeitarTokenSemAssinaturaComAlgNoneAsync()
    {
        var payload = (await EntrarAsync()).Split('.')[1];
        var cabecalhoNone = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");

        var resposta = await GetEuAsync($"{cabecalhoNone}.{payload}.");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestCase("outro-emissor", "castwork-app")]
    [TestCase("castwork-api", "outra-audiencia")]
    public async Task DeveRejeitarTokenDeOutroEmissorOuAudienciaMesmoComAChaveCertaAsync(string emissor, string audiencia)
    {
        var opcoes = AmbienteDeTeste.Obter<IOptions<JwtOptions>>().Value;
        var token = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = emissor,
            Audience = audiencia,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [CastworkClaims.UsuarioId] = "1" },
            SigningCredentials = new SigningCredentials(ParametrosDeValidacao.Chave(opcoes), ParametrosDeValidacao.Algoritmo),
        });

        var resposta = await GetEuAsync(token);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> EntrarAsync()
    {
        var usuarios = Usar(new UsuarioBuilder());
        _usuarioId = await usuarios.SendAsync(await usuarios.CustomizeCommand(c => (c.Nome, c.Email) = ("Ana Lima", "ana@atelie.com")).BuildCommandAsync());
        return (await AmbienteDeTeste.SendAsync(new LoginCommand { Email = "ana@atelie.com", Senha = UsuarioBuilder.SenhaPadrao })).AccessToken;
    }

    private Task<HttpResponseMessage> GetEuAsync(string token)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/auth/eu", UriKind.Relative));
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _http.SendAsync(requisicao);
    }

    private sealed record EuDto(long UsuarioId, string Nome, bool IsAdmin);
}
