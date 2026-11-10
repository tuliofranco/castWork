using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Precificacao;

/// <summary>
/// Preços do ateliê como a tela usa: escrita pelo REST (<c>api/precificacao</c>) e leitura pelo GraphQL.
/// </summary>
public class PrecificacaoEndpointsTests : TesteDeIntegracao
{
    private const string ConsultaPrecos = """
        {
          precosDoAtelie {
            cotacoes { metal atual { valor registradaEm registradaPor } }
            ligas { id nome metal teor densidade }
            parametros { perdaPercentual valorHora markup taxasPercentual arredondarPara }
          }
        }
        """;

    private const string ConsultaHistorico = """
        query ($metal: Metal!, $primeiros: Int) {
          historicoDeCotacoes(metal: $metal, first: $primeiros) {
            totalCount
            pageInfo { hasNextPage }
            nodes { valor registradaEm registradaPor }
          }
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
    public void DisposeHttp() => _http.Dispose();

    [Test]
    public async Task DeveAtualizarCotacaoELerOHistoricoComAutorAsync()
    {
        var (admin, nome) = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());

        var primeira = await PostAsync(admin, "/api/precificacao/cotacoes", new { metal = "Ouro", valor = 600m });
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromMinutes(5));
        var segunda = await PostAsync(admin, "/api/precificacao/cotacoes", new { metal = "Ouro", valor = 612.35m });
        var precos = await admin.ExecutarAsync(ConsultaPrecos);
        var pagina = await admin.ExecutarAsync(ConsultaHistorico, new { metal = "OURO", primeiros = 1 });
        var prata = await admin.ExecutarAsync(ConsultaHistorico, new { metal = "PRATA" });

        (primeira.StatusCode, segunda.StatusCode).Should().Be((HttpStatusCode.Created, HttpStatusCode.Created));
        precos["errors"].Should().BeNull();
        var ouro = precos["data"]!["precosDoAtelie"]!["cotacoes"]![0]!;
        (ouro["metal"]!.GetValue<string>(), ouro["atual"]!["valor"]!.GetValue<decimal>(), ouro["atual"]!["registradaPor"]!.GetValue<string>())
            .Should().Be(("OURO", 612.35m, nome));
        precos["data"]!["precosDoAtelie"]!["cotacoes"]![1]!["atual"].Should().BeNull("a prata ainda não tem cotação");
        var historico = pagina["data"]!["historicoDeCotacoes"]!;
        historico["totalCount"]!.GetValue<int>().Should().Be(2);
        historico["pageInfo"]!["hasNextPage"]!.GetValue<bool>().Should().BeTrue();
        historico["nodes"]![0]!["valor"]!.GetValue<decimal>().Should().Be(612.35m, "a mais recente vem primeiro");
        prata["data"]!["historicoDeCotacoes"]!["totalCount"]!.GetValue<int>().Should().Be(0);
    }

    [Test]
    public async Task DeveCriarAlterarEDesativarLigaPelaApiAsync()
    {
        var (admin, _) = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());

        var criada = await PostAsync(admin, "/api/precificacao/ligas", new { nome = "Ouro 18k vermelho", metal = "Ouro", teor = "750", densidade = 15.2m });
        var id = (await criada.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var alterada = await SendAsync(admin, HttpMethod.Put, $"/api/precificacao/ligas/{id}", new { nome = "Ouro 18k vermelho", metal = "Ouro", teor = "750", densidade = 15.25m });
        var desativada = await SendAsync(admin, HttpMethod.Post, "/api/precificacao/ligas/2/desativar", null);
        var invalida = await PostAsync(admin, "/api/precificacao/ligas", new { nome = "Sem densidade", metal = "Prata", teor = "925", densidade = 0 });
        var ligas = (await admin.ExecutarAsync(ConsultaPrecos))["data"]!["precosDoAtelie"]!["ligas"]!.AsArray();

        (criada.StatusCode, alterada.StatusCode, desativada.StatusCode, invalida.StatusCode)
            .Should().Be((HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.BadRequest));
        ligas.Select(l => l!["id"]!.GetValue<long>()).Should().Contain(id).And.NotContain(2);
        ligas.Single(l => l!["id"]!.GetValue<long>() == id)!["densidade"]!.GetValue<decimal>().Should().Be(15.25m);
    }

    [Test]
    public async Task DeveSalvarERecusarParametrosPelaApiAsync()
    {
        var (admin, _) = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());

        var salvo = await SendAsync(admin, HttpMethod.Put, "/api/precificacao/parametros", new { perdaPercentual = 8, valorHora = 90, markup = 1.8, taxasPercentual = 5, arredondarPara = 10 });
        var recusado = await SendAsync(admin, HttpMethod.Put, "/api/precificacao/parametros", new { perdaPercentual = 8, valorHora = 0, markup = 1.8, taxasPercentual = 5, arredondarPara = 10 });
        var parametros = (await admin.ExecutarAsync(ConsultaPrecos))["data"]!["precosDoAtelie"]!["parametros"]!;

        (salvo.StatusCode, recusado.StatusCode).Should().Be((HttpStatusCode.NoContent, HttpStatusCode.BadRequest));
        parametros["valorHora"]!.GetValue<decimal>().Should().Be(90m);
    }

    [Test]
    public async Task DeveNegarLeituraSemConsultarEEscritaSemEditarAsync()
    {
        var leitor = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Consultar).BuildAsync();
        var outro = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar).BuildAsync();
        var (comConsultar, _) = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(leitor));
        var (semRegra, _) = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(outro));

        var lidoPeloLeitor = await comConsultar.ExecutarAsync(ConsultaPrecos);
        var escritaDoLeitor = await PostAsync(comConsultar, "/api/precificacao/cotacoes", new { metal = "Ouro", valor = 600m });
        var negado = await semRegra.ExecutarAsync(ConsultaPrecos);
        var historicoNegado = await semRegra.ExecutarAsync(ConsultaHistorico, new { metal = "OURO" });
        var anonimo = await new ClienteGraphQl(_http, null).ExecutarAsync(ConsultaPrecos);

        lidoPeloLeitor["errors"].Should().BeNull();
        escritaDoLeitor.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ClienteGraphQl.CodigosDeErro(negado).Should().Equal("AUTH_NOT_AUTHORIZED");
        ClienteGraphQl.CodigosDeErro(historicoNegado).Should().Equal("AUTH_NOT_AUTHORIZED");
        ClienteGraphQl.CodigosDeErro(anonimo).Should().Equal("AUTH_NOT_AUTHENTICATED");
    }

    private async Task<(ClienteGraphQl Cliente, string Nome)> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return (new ClienteGraphQl(_http, sessao.AccessToken), command.Nome);
    }

    private Task<HttpResponseMessage> PostAsync(ClienteGraphQl cliente, string url, object corpo) => SendAsync(cliente, HttpMethod.Post, url, corpo);

    private async Task<HttpResponseMessage> SendAsync(ClienteGraphQl cliente, HttpMethod metodo, string url, object? corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(url, UriKind.Relative)) { Content = corpo is null ? null : JsonContent.Create(corpo) };
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await _http.SendAsync(requisicao);
    }

    private sealed record IdLido(long Id);
}
