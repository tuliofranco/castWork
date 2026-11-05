using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Atelie;

/// <summary>
/// Marca do ateliê pela API: dados e logo, como a tela de configurações usa.
/// </summary>
public class AtelieEndpointsTests : TesteDeIntegracao
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

    private HttpClient _http = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
        var usuarios = Usar(new UsuarioBuilder()).Admin();
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
    }

    [TearDown]
    public void TearDown() => _http.Dispose();

    [Test]
    public async Task DeveAtualizarAMarcaAsync()
    {
        var resposta = await _http.PutAsJsonAsync("/api/atelie", new { nome = "Ateliê Aurora", assinatura = "Joias autorais", corMarca = "#8a5a44", instagram = "@aurora", site = "aurora.com" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent, await resposta.Content.ReadAsStringAsync());
        var configuracao = await AmbienteDeTeste.SendAsync(new GetConfiguracaoAtelieQuery());
        configuracao.Should().BeEquivalentTo(new { Nome = "Ateliê Aurora", Assinatura = "Joias autorais", CorMarca = "#8A5A44", Instagram = "aurora", Site = "aurora.com" });
    }

    [Test]
    public async Task DeveRecusarCorInvalidaAsync()
    {
        var resposta = await _http.PutAsJsonAsync("/api/atelie", new { nome = "Ateliê", corMarca = "verde" });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeveEnviarLerERemoverOLogoAsync()
    {
        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(Png);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formulario.Add(arquivo, "arquivo", "logo.png");

        var envio = await _http.PutAsync("/api/atelie/logo", formulario);
        var leitura = await _http.GetAsync("/api/atelie/logo");
        var remocao = await _http.DeleteAsync("/api/atelie/logo");
        var depois = await _http.GetAsync("/api/atelie/logo");

        (envio.StatusCode, leitura.StatusCode, remocao.StatusCode, depois.StatusCode)
            .Should().Be((HttpStatusCode.NoContent, HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NoContent));
        leitura.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await leitura.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Test]
    public async Task DeveRecusarLogoQueNaoEImagemAsync()
    {
        using var formulario = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent("<svg onload=alert(1)>"u8.ToArray());
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formulario.Add(arquivo, "arquivo", "logo.png");

        var envio = await _http.PutAsync("/api/atelie/logo", formulario);

        envio.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
