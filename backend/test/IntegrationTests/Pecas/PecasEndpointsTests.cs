using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;
using Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

/// <summary>
/// Biblioteca de peças pelo GraphQL: só consulta, sem valor, com o histórico de uso (projetos, versões e o valor em cada
/// projeto, este só com <c>precificacao.ler</c>). A escrita pelo REST (<c>api/pecas</c>) continua, sem valor.
/// </summary>
public class PecasEndpointsTests : TesteDeIntegracao
{
    private const string ConsultaComHistorico = """
        query ($id: Long!) {
          peca(id: $id) { id nome historicoDeUso { projetoId projeto pecaDoProjetoId valor versoes { versaoId numero nome status quantidade } } }
        }
        """;

    private const string ConsultaHistoricoSemValor = """
        query ($id: Long!) {
          peca(id: $id) { id nome historicoDeUso { projeto versoes { numero } } }
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
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database.ExecuteSqlRawAsync("UPDATE projeto_pecas SET peca_id = NULL"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.ExecuteDeleteAsync());
        _http.Dispose();
    }

    [Test]
    public async Task DeveCriarEAlterarPecaPelaApiSemValorAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());

        var criada = await SendAsync(admin, HttpMethod.Post, "/api/pecas", new { nome = "Turmalina 4 mm", tipo = "Pedra" });
        var id = (await criada.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var alterada = await SendAsync(admin, HttpMethod.Put, $"/api/pecas/{id}", new { nome = "Turmalina 5 mm", tipo = "Pedra" });
        var lida = await admin.ExecutarAsync("query ($id: Long!) { peca(id: $id) { nome historicoDeUso { projeto } } }", new { id });
        var custo = await admin.ExecutarAsync("query ($id: Long!) { peca(id: $id) { custo } }", new { id });

        (criada.StatusCode, alterada.StatusCode).Should().Be((HttpStatusCode.Created, HttpStatusCode.NoContent));
        lida["errors"].Should().BeNull();
        lida["data"]!["peca"]!["nome"]!.GetValue<string>().Should().Be("Turmalina 5 mm");
        lida["data"]!["peca"]!["historicoDeUso"]!.AsArray().Should().BeEmpty("nenhum projeto usa a peça");
        custo["errors"].Should().NotBeNull("a peça da biblioteca não tem mais custo");
    }

    [Test]
    public async Task DeveMostrarOHistoricoDeUsoComOValorDeCadaProjetoAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var colar = await Usar(new ProjetoBuilder()).CustomizeCommand(c => c.Nome = "Colar Raízes").BuildAsync();
        var pulseira = await Usar(new ProjetoBuilder()).CustomizeCommand(c => c.Nome = "Pulseira Mar").BuildAsync();
        var pecasDoColar = Usar(new PecaDoProjetoBuilder(colar).ComValor(80m).Publicada());
        var turmalina = await pecasDoColar.CriarAsync();
        var bibliotecaId = pecasDoColar.PecasDaBiblioteca.Single();
        var v1 = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = colar, Nome = "Base" });
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = colar, VersaoId = v1, PecaDoProjetoId = turmalina, Quantidade = 3 });
        var naPulseira = await AmbienteDeTeste.SendAsync(new ImportarPecaDaBibliotecaCommand { ProjetoId = pulseira, PecaId = bibliotecaId });
        await AmbienteDeTeste.SendAsync(new DefinirValorDaPecaDoProjetoCommand { ProjetoId = pulseira, PecaDoProjetoId = naPulseira, Valor = 95m });

        var resposta = await admin.ExecutarAsync(ConsultaComHistorico, new { id = bibliotecaId });
        var biblioteca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_name = 'pecas' AND column_name = 'custo'")
            .SingleAsync());

        resposta["errors"].Should().BeNull();
        var historico = resposta["data"]!["peca"]!["historicoDeUso"]!.AsArray();
        historico.Select(u => (u!["projeto"]!.GetValue<string>(), u["pecaDoProjetoId"]!.GetValue<long>(), u["valor"]!.GetValue<decimal>()))
            .Should().Equal(("Pulseira Mar", naPulseira, 95m), ("Colar Raízes", turmalina, 80m));
        historico[0]!["versoes"]!.AsArray().Should().BeEmpty("a pulseira importou a peça mas ainda não a usou numa versão");
        historico[1]!["versoes"]!.AsArray().Select(v => (v!["versaoId"]!.GetValue<long>(), v["nome"]!.GetValue<string>(), v["status"]!.GetValue<string>(), v["quantidade"]!.GetValue<int>()))
            .Should().Equal((v1, "Base", "EM_DESENVOLVIMENTO", 3));
        biblioteca.Should().Be(0, "a biblioteca não guarda valor");
    }

    [Test]
    public async Task DeveProtegerOValorDoHistoricoPelaPrecificacaoEOHistoricoPelaLeituraDeProjetosAsync()
    {
        var projeto = await Usar(new ProjetoBuilder()).BuildAsync();
        var pecas = Usar(new PecaDoProjetoBuilder(projeto).ComValor(80m).Publicada());
        await pecas.CriarAsync();
        var id = pecas.PecasDaBiblioteca.Single();
        var semPrecificacao = await EntrarComAsync(Regras.Pecas.Consultar, Regras.Projetos.Consultar);
        var semProjetos = await EntrarComAsync(Regras.Pecas.Consultar, Regras.Precificacao.Consultar);

        var valorNegado = await semPrecificacao.ExecutarAsync(ConsultaComHistorico, new { id });
        var semPedirValor = await semPrecificacao.ExecutarAsync(ConsultaHistoricoSemValor, new { id });
        var historicoNegado = await semProjetos.ExecutarAsync(ConsultaHistoricoSemValor, new { id });

        ClienteGraphQl.CodigosDeErro(valorNegado).Should().OnlyContain(c => c == "AUTH_NOT_AUTHORIZED").And.NotBeEmpty();
        valorNegado["data"]?["peca"]?["historicoDeUso"]?[0]?["valor"].Should().BeNull("o valor não sai na resposta");
        semPedirValor["errors"].Should().BeNull();
        semPedirValor["data"]!["peca"]!["historicoDeUso"]!.AsArray().Should().ContainSingle();
        ClienteGraphQl.CodigosDeErro(historicoNegado).Should().Equal("AUTH_NOT_AUTHORIZED");
        historicoNegado["data"]!["peca"]!["historicoDeUso"].Should().BeNull("sem ler projetos, o histórico não sai");
        historicoNegado["data"]!["peca"]!["nome"].Should().NotBeNull("o resto da peça continua legível");
    }

    private async Task<ClienteGraphQl> EntrarComAsync(params string[] regras)
    {
        var perfil = await Usar(new PerfilBuilder()).ComRegras(regras).BuildAsync();
        return await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(perfil));
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
