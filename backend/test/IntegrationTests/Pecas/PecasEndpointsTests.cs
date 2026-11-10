using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

/// <summary>
/// Biblioteca de peças como a tela usa: escrita pelo REST (<c>api/pecas</c>) e leitura pelo GraphQL,
/// com o custo visível só para quem tem <c>precificacao.ler</c>.
/// </summary>
public class PecasEndpointsTests : TesteDeIntegracao
{
    private const string ConsultaComCusto = """
        query ($id: Long!) {
          peca(id: $id) { id nome custo }
          pecas(first: 10) { nodes { id custo } }
        }
        """;

    private const string ConsultaSemCusto = """
        query ($id: Long!) {
          peca(id: $id) { id nome }
        }
        """;

    private HttpClient _http = null!;

    [SetUp]
    public void SetUp()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
    }

    [TearDown]
    public async Task RemoverCriadosPeloHttpAsync()
    {
        // Peças criadas por HTTP não passam por builder: remove aqui.
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.ExecuteDeleteAsync());
        _http.Dispose();
    }

    [Test]
    public async Task DeveCriarEAlterarPecaComCustoELerOCustoPeloGraphQlAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());

        var criada = await SendAsync(admin, HttpMethod.Post, "/api/pecas", new { nome = "Turmalina 4 mm", tipo = "Pedra", custo = 85.5m });
        var id = (await criada.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var semCusto = await SendAsync(admin, HttpMethod.Post, "/api/pecas", new { nome = "Fecho lagosta", tipo = "Fecho" });
        var lidaAoCriar = await admin.ExecutarAsync(ConsultaComCusto, new { id });
        var alterada = await SendAsync(admin, HttpMethod.Put, $"/api/pecas/{id}", new { nome = "Turmalina 5 mm", tipo = "Pedra", custo = 110m });
        var negativa = await SendAsync(admin, HttpMethod.Put, $"/api/pecas/{id}", new { nome = "Turmalina 5 mm", tipo = "Pedra", custo = -1m });
        var lidaAoAlterar = await admin.ExecutarAsync(ConsultaComCusto, new { id });

        (criada.StatusCode, semCusto.StatusCode, alterada.StatusCode, negativa.StatusCode)
            .Should().Be((HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.BadRequest));
        lidaAoCriar["errors"].Should().BeNull();
        lidaAoCriar["data"]!["peca"]!["custo"]!.GetValue<decimal>().Should().Be(85.5m);
        lidaAoCriar["data"]!["pecas"]!["nodes"]!.AsArray().Select(p => p!["custo"]?.GetValue<decimal>())
            .Should().BeEquivalentTo(new decimal?[] { 85.5m, null }, "a peça sem custo responde null");
        (lidaAoAlterar["data"]!["peca"]!["nome"]!.GetValue<string>(), lidaAoAlterar["data"]!["peca"]!["custo"]!.GetValue<decimal>())
            .Should().Be(("Turmalina 5 mm", 110m));
    }

    [Test]
    public async Task DeveEsconderOCustoDeQuemNaoConsultaAPrecificacaoAsync()
    {
        var id = await Usar(new PecaBuilder()).CustomizeCommand(c => (c.Nome, c.Tipo, c.Custo) = ("Turmalina", Castwork.Domain.Entities.Pecas.TipoPeca.Pedra, 85.5m)).BuildAsync();
        var soPecas = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar, Regras.Pecas.Alterar).BuildAsync();
        var comPrecificacao = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar, Regras.Precificacao.Consultar).BuildAsync();
        var semPrecificacao = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(soPecas));
        var leitor = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(comPrecificacao));

        var negado = await semPrecificacao.ExecutarAsync(ConsultaComCusto, new { id });
        var semPedirCusto = await semPrecificacao.ExecutarAsync(ConsultaSemCusto, new { id });
        var alteracao = await SendAsync(semPrecificacao, HttpMethod.Put, $"/api/pecas/{id}", new { nome = "Turmalina", tipo = "Pedra", custo = 1m });
        var permitido = await leitor.ExecutarAsync(ConsultaComCusto, new { id });

        ClienteGraphQl.CodigosDeErro(negado).Should().OnlyContain(c => c == "AUTH_NOT_AUTHORIZED").And.NotBeEmpty();
        negado["data"]?["peca"]?["custo"].Should().BeNull("o valor do custo não sai na resposta");
        negado.ToJsonString().Should().NotContain("85.5");
        semPedirCusto["errors"].Should().BeNull("o resto da peça continua legível");
        semPedirCusto["data"]!["peca"]!["nome"]!.GetValue<string>().Should().Be("Turmalina");
        alteracao.StatusCode.Should().Be(HttpStatusCode.NoContent);
        permitido["errors"].Should().BeNull();
        permitido["data"]!["peca"]!["custo"]!.GetValue<decimal>().Should().Be(85.5m, "quem não vê o custo não o altera");
    }

    private async Task<ClienteGraphQl> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(_http, sessao.AccessToken);
    }

    private async Task<HttpResponseMessage> SendAsync(ClienteGraphQl cliente, HttpMethod metodo, string url, object corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(url, UriKind.Relative)) { Content = JsonContent.Create(corpo) };
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await _http.SendAsync(requisicao);
    }

    private sealed record IdLido(long Id);
}
