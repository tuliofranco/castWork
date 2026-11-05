using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.GraphQl;

public class PecasGraphQlTests : TesteDeIntegracao
{
    private const string ConsultaPecas = """
        query ($primeiros: Int, $ordem: [PecaOrdenacao!], $filtro: PecaFiltro) {
          pecas(first: $primeiros, order: $ordem, where: $filtro) {
            totalCount
            nodes { id titulo status proximosStatus cliente { id nome documento } }
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
    public async Task DeveListarPecasPorIdDecrescenteComClienteCarregadoPeloDataLoaderAsync()
    {
        var maria = await Usar(new ClienteBuilder()).CustomizeCommand(c => (c.Nome, c.Documento) = ("Maria Silva", "52998224725")).BuildAsync();
        var joao = await Usar(new ClienteBuilder()).CustomizeCommand(c => (c.Nome, c.Documento) = ("João Souza", null)).BuildAsync();
        var anel = await Usar(new PecaBuilder()).DoCliente(maria).CustomizeCommand(c => c.Titulo = "Anel").BuildAsync();
        var brinco = await Usar(new PecaBuilder()).DoCliente(joao).CustomizeCommand(c => c.Titulo = "Brinco").BuildAsync();
        var colar = await Usar(new PecaBuilder()).DoCliente(maria).CustomizeCommand(c => c.Titulo = "Colar").BuildAsync();

        var resposta = await (await ComoAdminAsync()).ExecutarAsync(ConsultaPecas);

        resposta["errors"].Should().BeNull();
        resposta["data"]!["pecas"]!["totalCount"]!.GetValue<int>().Should().Be(3);
        ClienteGraphQl.Ler<List<PecaLida>>(resposta["data"]!["pecas"]!["nodes"]).Should().BeEquivalentTo(
            new[]
            {
                new PecaLida(colar, "Colar", "BRIEFING", ["ORCAMENTO", "CANCELADA"], new ClienteLido(maria, "Maria Silva", "529.982.247-25")),
                new PecaLida(brinco, "Brinco", "BRIEFING", ["ORCAMENTO", "CANCELADA"], new ClienteLido(joao, "João Souza", null)),
                new PecaLida(anel, "Anel", "BRIEFING", ["ORCAMENTO", "CANCELADA"], new ClienteLido(maria, "Maria Silva", "529.982.247-25")),
            },
            o => o.WithStrictOrdering());
    }

    [Test]
    public async Task DeveFiltrarPorClienteEOrdenarPorTituloAsync()
    {
        var maria = await Usar(new ClienteBuilder()).BuildAsync();
        var outra = await Usar(new ClienteBuilder()).BuildAsync();
        var pecas = Usar(new PecaBuilder());
        await pecas.DoCliente(maria).CustomizeCommand(c => c.Titulo = "Pulseira").BuildAsync();
        await pecas.DoCliente(maria).CustomizeCommand(c => c.Titulo = "Aliança").BuildAsync();
        await Usar(new PecaBuilder()).DoCliente(outra).CustomizeCommand(c => c.Titulo = "Brinco").BuildAsync();

        var resposta = await (await ComoAdminAsync()).ExecutarAsync(
            ConsultaPecas,
            new { ordem = new[] { new { titulo = "ASC" } }, filtro = new { clienteId = new { eq = maria } } });

        resposta["errors"].Should().BeNull();
        resposta["data"]!["pecas"]!["nodes"]!.AsArray().Select(n => n!["titulo"]!.GetValue<string>()).Should().Equal("Aliança", "Pulseira");
    }

    [Test]
    public async Task DeveBuscarPorTermoNoTituloOuNoClienteSemDiferenciarMaiusculasAsync()
    {
        var joao = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "João Souza").BuildAsync();
        await Usar(new PecaBuilder()).CustomizeCommand(c => c.Titulo = "Anel de formatura").BuildAsync();
        await Usar(new PecaBuilder()).DoCliente(joao).CustomizeCommand(c => c.Titulo = "Brinco").BuildAsync();
        var admin = await ComoAdminAsync();

        var porTitulo = await admin.ExecutarAsync("query ($t: String) { pecas(termo: $t) { nodes { titulo } } }", new { t = "FORMATURA" });
        var porCliente = await admin.ExecutarAsync("query ($t: String) { pecas(termo: $t) { nodes { titulo } } }", new { t = "souza" });

        porTitulo["data"]!["pecas"]!["nodes"]!.AsArray().Select(n => n!["titulo"]!.GetValue<string>()).Should().Equal("Anel de formatura");
        porCliente["data"]!["pecas"]!["nodes"]!.AsArray().Select(n => n!["titulo"]!.GetValue<string>()).Should().Equal("Brinco");
    }

    [Test]
    public async Task DeveResponder401ParaTokenExpiradoParaOFrontRenovarAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(-1);
        var vencido = await ComoAdminAsync();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri("/graphql", UriKind.Relative))
        {
            Content = JsonContent.Create(new { query = "{ pecas { totalCount } }" }),
        };
        requisicao.Headers.Authorization = new("Bearer", vencido.Token);

        using var resposta = await _http.SendAsync(requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "200 com erro de autorização não dispararia o refresh do front");
    }

    [Test]
    public async Task DeveNegarConsultaSemTokenEParaPerfilSemARegraAsync()
    {
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Clientes.Consultar).BuildAsync();
        var semRegra = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(perfil));

        var anonimo = await new ClienteGraphQl(_http, null).ExecutarAsync(ConsultaPecas);
        var negado = await semRegra.ExecutarAsync(ConsultaPecas);

        ClienteGraphQl.CodigosDeErro(anonimo).Should().Equal("AUTH_NOT_AUTHENTICATED");
        ClienteGraphQl.CodigosDeErro(negado).Should().Equal("AUTH_NOT_AUTHORIZED");
        negado["data"]?["pecas"].Should().BeNull();
    }

    [Test]
    public async Task DevePermitirPerfilComARegraAsync()
    {
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Pecas.Consultar).BuildAsync();
        var comRegra = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(perfil));

        var resposta = await comRegra.ExecutarAsync(ConsultaPecas);

        resposta["errors"].Should().BeNull();
    }

    [Test]
    public async Task DeveRecusarPaginaMaiorQueOLimiteAsync()
    {
        var resposta = await (await ComoAdminAsync()).ExecutarAsync(ConsultaPecas, new { primeiros = GraphQlConfiguration.TamanhoMaximoDaPagina + 1 });

        resposta["errors"].Should().NotBeNull();
        resposta["data"]?["pecas"].Should().BeNull();
    }

    [Test]
    public async Task DeveRecusarConsultaMaisProfundaQueOLimiteAsync()
    {
        const string profunda = "{ pecas { nodes { cliente { pecas { cliente { pecas { cliente { pecas { cliente { nome } } } } } } } } } }";

        var resposta = await (await ComoAdminAsync()).ExecutarAsync(profunda);

        resposta["data"].Should().BeNull();
        resposta["errors"]!.AsArray().Select(e => e!["extensions"]?["allowedExecutionDepth"]?.GetValue<int>())
            .Should().Contain(GraphQlConfiguration.ProfundidadeMaxima);
    }

    [Test]
    public async Task DeveRecusarIntrospeccaoForaDeDesenvolvimentoAsync()
    {
        var resposta = await (await ComoAdminAsync()).ExecutarAsync("{ __schema { types { name } } }");

        ClienteGraphQl.CodigosDeErro(resposta).Should().Contain("HC0046");
    }

    [Test]
    public async Task DeveRecusarBatchingDeOperacoesAsync()
    {
        var token = (await ComoAdminAsync()).Token;
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri("/graphql", UriKind.Relative))
        {
            Content = JsonContent.Create(new[] { new { query = "{ pecas { totalCount } }" }, new { query = "{ clientes { totalCount } }" } }),
        };
        requisicao.Headers.Authorization = new("Bearer", token);

        using var resposta = await _http.SendAsync(requisicao);

        resposta.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeveLerConfiguracaoDoAtelieDelegandoAoMediatRAsync()
    {
        var resposta = await (await ComoAdminAsync()).ExecutarAsync("{ configuracaoAtelie { nome valorHoraPadrao possuiLogo } }");

        resposta["errors"].Should().BeNull();
        resposta["data"]!["configuracaoAtelie"]!["nome"]!.GetValue<string>().Should().Be("Meu ateliê");
    }

    private Task<ClienteGraphQl> ComoAdminAsync() => EntrarAsync(Usar(new UsuarioBuilder()).Admin());

    private async Task<ClienteGraphQl> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(_http, sessao.AccessToken);
    }

    private sealed record ClienteLido(long Id, string Nome, string? Documento);

    private sealed record PecaLida(long Id, string Titulo, string Status, string[] ProximosStatus, ClienteLido Cliente);
}
