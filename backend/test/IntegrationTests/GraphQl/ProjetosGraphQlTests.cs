using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DuplicarVersao;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.GraphQl;

public class ProjetosGraphQlTests : TesteDeIntegracao
{
    private const string ConsultaProjetos = """
        query ($termo: String, $primeiros: Int) {
          projetos(termo: $termo, first: $primeiros) {
            totalCount
            nodes { id nome cliente { nome } versoes { numero nome status itens { quantidade peca { nome tipo teor } } } }
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
    public async Task DeveListarProjetosComClienteVersoesEComposicaoAsync()
    {
        var maria = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "Maria Silva").BuildAsync();
        var elo = await Usar(new PecaBuilder()).CustomizeCommand(c => (c.Nome, c.Tipo, c.Teor) = ("Elo oval", Domain.Entities.Pecas.TipoPeca.Elo, "18k")).BuildAsync();
        var colar = await Usar(new ProjetoBuilder()).DoCliente(maria).CustomizeCommand(c => c.Nome = "Colar Raízes").BuildAsync();
        var autoral = await Usar(new ProjetoBuilder()).CustomizeCommand(c => c.Nome = "Pulseira autoral").BuildAsync();
        var v1 = await V1Async(colar);
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = colar, VersaoId = v1, PecaId = elo, Quantidade = 12 });
        await AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = colar, VersaoId = v1 });
        await AmbienteDeTeste.SendAsync(new DuplicarVersaoCommand { ProjetoId = colar, VersaoId = v1, Nome = "Com pedra" });

        var resposta = await (await ComoAdminAsync()).ExecutarAsync(ConsultaProjetos);

        resposta["errors"].Should().BeNull();
        resposta["data"]!["projetos"]!["totalCount"]!.GetValue<int>().Should().Be(2);
        var nodes = resposta["data"]!["projetos"]!["nodes"]!.AsArray();
        nodes.Select(n => n!["nome"]!.GetValue<string>()).Should().Equal("Pulseira autoral", "Colar Raízes");
        nodes[0]!["cliente"].Should().BeNull();
        nodes[0]!["versoes"]!.AsArray().Should().BeEmpty("o projeto nasce sem versões");
        var versoesDoColar = ClienteGraphQl.Ler<List<VersaoLida>>(nodes[1]!["versoes"]);
        nodes[1]!["cliente"]!["nome"]!.GetValue<string>().Should().Be("Maria Silva");
        versoesDoColar.Should().BeEquivalentTo(
            new[]
            {
                new VersaoLida(2, "Com pedra", "EM_DESENVOLVIMENTO", [new ItemLido(12, new PecaLida("Elo oval", "ELO", 750))]),
                new VersaoLida(1, "Base", "PUBLICADA", [new ItemLido(12, new PecaLida("Elo oval", "ELO", 750))]),
            },
            o => o.WithStrictOrdering());
        autoral.Should().BePositive();
    }

    [TestCase("COLAR", "Colar Raízes")]
    [TestCase("maria", "Colar Raízes")]
    [TestCase("autoral", "Pulseira autoral")]
    public async Task DeveBuscarProjetosPorNomeOuClienteAsync(string termo, string esperado)
    {
        var maria = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "Maria Silva").BuildAsync();
        await Usar(new ProjetoBuilder()).DoCliente(maria).CustomizeCommand(c => c.Nome = "Colar Raízes").BuildAsync();
        await Usar(new ProjetoBuilder()).CustomizeCommand(c => c.Nome = "Pulseira autoral").BuildAsync();

        var resposta = await (await ComoAdminAsync()).ExecutarAsync(ConsultaProjetos, new { termo });

        resposta["data"]!["projetos"]!["nodes"]!.AsArray().Select(n => n!["nome"]!.GetValue<string>()).Should().Equal(esperado);
    }

    [Test]
    public async Task DeveBuscarPecasDaBibliotecaPorNomeOuMaterialEFiltrarPorTipoAsync()
    {
        await Usar(new PecaBuilder()).CustomizeCommand(c => (c.Nome, c.Tipo, c.Material) = ("Elo oval", Domain.Entities.Pecas.TipoPeca.Elo, "Ouro amarelo")).BuildAsync();
        await Usar(new PecaBuilder()).CustomizeCommand(c => (c.Nome, c.Tipo, c.Material) = ("Turmalina", Domain.Entities.Pecas.TipoPeca.Pedra, "Turmalina")).BuildAsync();
        await Usar(new PecaBuilder()).CustomizeCommand(c => (c.Nome, c.Tipo, c.Material) = ("Fecho", Domain.Entities.Pecas.TipoPeca.Fecho, "Ouro amarelo")).BuildAsync();
        var admin = await ComoAdminAsync();

        var porMaterial = await admin.ExecutarAsync("query ($t: String) { pecas(termo: $t) { nodes { nome } } }", new { t = "OURO" });
        var porTipo = await admin.ExecutarAsync("{ pecas(where: { tipo: { eq: PEDRA } }) { nodes { nome } } }");

        porMaterial["data"]!["pecas"]!["nodes"]!.AsArray().Select(n => n!["nome"]!.GetValue<string>()).Should().Equal("Elo oval", "Fecho");
        porTipo["data"]!["pecas"]!["nodes"]!.AsArray().Select(n => n!["nome"]!.GetValue<string>()).Should().Equal("Turmalina");
    }

    [Test]
    public async Task DeveNegarConsultaSemTokenEParaPerfilSemARegraAsync()
    {
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Clientes.Consultar).BuildAsync();
        var semRegra = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(perfil));

        var anonimo = await new ClienteGraphQl(_http, null).ExecutarAsync(ConsultaProjetos);
        var negado = await semRegra.ExecutarAsync(ConsultaProjetos);

        ClienteGraphQl.CodigosDeErro(anonimo).Should().Equal("AUTH_NOT_AUTHENTICATED");
        ClienteGraphQl.CodigosDeErro(negado).Should().Equal("AUTH_NOT_AUTHORIZED");
    }

    [Test]
    public async Task DevePermitirPerfilComARegraAsync()
    {
        var perfil = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        var comRegra = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(perfil));

        var resposta = await comRegra.ExecutarAsync(ConsultaProjetos);

        resposta["errors"].Should().BeNull();
    }

    [Test]
    public async Task DeveResponder401ParaTokenExpiradoParaOFrontRenovarAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(-1);
        var vencido = await ComoAdminAsync();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri("/graphql", UriKind.Relative))
        {
            Content = JsonContent.Create(new { query = "{ projetos { totalCount } }" }),
        };
        requisicao.Headers.Authorization = new("Bearer", vencido.Token);

        using var resposta = await _http.SendAsync(requisicao);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeveRecusarPaginaMaiorQueOLimiteAsync()
    {
        var resposta = await (await ComoAdminAsync()).ExecutarAsync(ConsultaProjetos, new { primeiros = GraphQlConfiguration.TamanhoMaximoDaPagina + 1 });

        resposta["errors"].Should().NotBeNull();
        resposta["data"]?["projetos"].Should().BeNull();
    }

    [Test]
    public async Task DeveRecusarConsultaMaisProfundaQueOLimiteAsync()
    {
        const string profunda = "{ projetos { nodes { cliente { projetos { cliente { projetos { cliente { projetos { cliente { nome } } } } } } } } } }";

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
            Content = JsonContent.Create(new[] { new { query = "{ projetos { totalCount } }" }, new { query = "{ clientes { totalCount } }" } }),
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

    /// <summary>O projeto nasce sem versões; cria a primeira.</summary>
    private static Task<long> V1Async(long projetoId)
        => AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });

    private Task<ClienteGraphQl> ComoAdminAsync() => EntrarAsync(Usar(new UsuarioBuilder()).Admin());

    private async Task<ClienteGraphQl> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(_http, sessao.AccessToken);
    }

    private sealed record PecaLida(string Nome, string Tipo, int? Teor);

    private sealed record ItemLido(int Quantidade, PecaLida Peca);

    private sealed record VersaoLida(int Numero, string Nome, string Status, ItemLido[] Itens);
}
