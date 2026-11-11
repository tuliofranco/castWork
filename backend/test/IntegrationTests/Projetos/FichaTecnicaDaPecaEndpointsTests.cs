using System.Net;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// A ficha técnica da peça em PDF (<c>api/projetos/{id}/pecas/{pecaId}/documentos/ficha-tecnica.pdf</c>), gerada na
/// hora pelo gerador de teste (que devolve o texto do documento), com o valor protegido por <c>precificacao.ler</c>.
/// </summary>
public class FichaTecnicaDaPecaEndpointsTests : TesteDeIntegracao
{
    private HttpClient _http = null!;

    [SetUp]
    public void SetUp()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
    }

    [TearDown]
    public void DisposeHttp() => _http.Dispose();

    [Test]
    public async Task DeveGerarAFichaTecnicaEmPdfComOValorAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var (projetoId, pecaId) = await PecaComValorAsync();

        var resposta = await BaixarAsync(admin, projetoId, pecaId);
        var texto = await TextoAsync(resposta);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        texto.Should().StartWith("%PDF").And.Contain("FICHA TÉCNICA").And.Contain("Status: Rascunho").And.Contain("Peso:");
        texto.Should().Contain("Valor no projeto: R$ 1.234,50");
    }

    [Test]
    public async Task DeveTirarOValorSemPrecificacaoLerAsync()
    {
        var (projetoId, pecaId) = await PecaComValorAsync();
        var soProjetos = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        var semPrecificacao = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(soProjetos));

        var resposta = await BaixarAsync(semPrecificacao, projetoId, pecaId);
        var texto = await TextoAsync(resposta);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        texto.Should().Contain("FICHA TÉCNICA").And.NotContain("Valor no projeto").And.NotContain("1.234,50");
    }

    [Test]
    public async Task DeveResponder404ParaPecaDeOutroProjetoAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var (_, pecaId) = await PecaComValorAsync();
        var outroProjeto = await Usar(new ProjetoBuilder()).BuildAsync();

        var resposta = await BaixarAsync(admin, outroProjeto, pecaId);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<string> TextoAsync(HttpResponseMessage resposta)
        => Encoding.UTF8.GetString(await resposta.Content.ReadAsByteArrayAsync());

    private static async Task<string> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        return (await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha })).AccessToken;
    }

    private async Task<(long ProjetoId, long PecaDoProjetoId)> PecaComValorAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecaId = await Usar(new PecaDoProjetoBuilder(projetoId).ComValor(1234.5m)).CriarAsync();
        return (projetoId, pecaId);
    }

    private async Task<HttpResponseMessage> BaixarAsync(string token, long projetoId, long pecaId)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/projetos/{projetoId}/pecas/{pecaId}/documentos/ficha-tecnica.pdf", UriKind.Relative));
        requisicao.Headers.Authorization = new("Bearer", token);
        return await _http.SendAsync(requisicao);
    }
}
