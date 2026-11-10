using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Domain.Entities.Precificacao;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Preços do projeto como a tela usa: leitura no <c>projeto(id) { precos }</c> do GraphQL e escrita pelo REST
/// (<c>PUT api/projetos/{id}/precos</c> e <c>POST api/projetos/{id}/precos/atualizar</c>).
/// </summary>
public class PrecosDoProjetoEndpointsTests : TesteDeIntegracao
{
    private const string ConsultaPrecos = """
        query ($id: Long!) {
          projeto(id: $id) {
            nome
            precos {
              herdadosEm
              alteradosEm
              cotacoes { metal valor cotadaEm }
              parametros { perdaPercentual valorHora markup taxasPercentual arredondarPara }
            }
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
    public async Task DeveCriarHerdandoEditarEAtualizarOsPrecosPelaApiAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        var criado = await SendAsync(admin, HttpMethod.Post, "/api/projetos", new { nome = "Colar Raízes" });
        var id = (await criado.Content.ReadFromJsonAsync<IdLido>())!.Id;
        var herdado = (await admin.ExecutarAsync(ConsultaPrecos, new { id }))["data"]!["projeto"]!["precos"]!;

        var editado = await SendAsync(admin, HttpMethod.Put, $"/api/projetos/{id}/precos", new
        {
            cotacoes = new[] { new { metal = "Ouro", valor = 655m }, new { metal = "Prata", valor = 6.8m } },
            perdaPercentual = 10,
            valorHora = 120,
            markup = 2.2,
            taxasPercentual = 5,
            arredondarPara = 50,
        });
        var recusado = await SendAsync(admin, HttpMethod.Put, $"/api/projetos/{id}/precos", new { cotacoes = new[] { new { metal = "Ouro", valor = 0 } }, valorHora = 120, markup = 2.2 });
        var aposEdicao = (await admin.ExecutarAsync(ConsultaPrecos, new { id }))["data"]!["projeto"]!["precos"]!;
        var atualizado = await SendAsync(admin, HttpMethod.Post, $"/api/projetos/{id}/precos/atualizar", null);
        var aposAtualizar = (await admin.ExecutarAsync(ConsultaPrecos, new { id }))["data"]!["projeto"]!["precos"]!;

        (criado.StatusCode, editado.StatusCode, recusado.StatusCode, atualizado.StatusCode)
            .Should().Be((HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.NoContent));
        herdado["cotacoes"]!.AsArray().Select(c => (c!["metal"]!.GetValue<string>(), c["valor"]!.GetValue<decimal>())).Should().Equal(("OURO", 640m));
        herdado["parametros"]!["markup"]!.GetValue<decimal>().Should().Be(1.8m);
        herdado["alteradosEm"].Should().BeNull();
        aposEdicao["cotacoes"]!.AsArray().Select(c => c!["valor"]!.GetValue<decimal>()).Should().Equal(655m, 6.8m);
        (aposEdicao["parametros"]!["valorHora"]!.GetValue<decimal>(), aposEdicao["parametros"]!["markup"]!.GetValue<decimal>()).Should().Be((120m, 2.2m));
        aposEdicao["alteradosEm"].Should().NotBeNull();
        aposAtualizar["cotacoes"]!.AsArray().Select(c => c!["valor"]!.GetValue<decimal>()).Should().Equal(640m);
        (aposAtualizar["parametros"]!["markup"]!.GetValue<decimal>(), aposAtualizar["alteradosEm"]).Should().Be((1.8m, null));

        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == id).ExecuteDeleteAsync());
    }

    [Test]
    public async Task DeveEsconderOsValoresENegarEdicaoSemARegraDePrecificacaoAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        var id = await Usar(new ProjetoBuilder()).BuildAsync();
        var soProjetos = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar, Regras.Projetos.Editar).BuildAsync();
        var leitor = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar, Regras.Precificacao.Consultar).BuildAsync();
        var semPrecificacao = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(soProjetos));
        var comConsultar = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(leitor));

        var negado = await semPrecificacao.ExecutarAsync(ConsultaPrecos, new { id });
        var lido = await comConsultar.ExecutarAsync(ConsultaPrecos, new { id });
        var edicaoSemRegra = await SendAsync(semPrecificacao, HttpMethod.Post, $"/api/projetos/{id}/precos/atualizar", null);
        var edicaoDoLeitor = await SendAsync(comConsultar, HttpMethod.Put, $"/api/projetos/{id}/precos", new { cotacoes = Array.Empty<object>(), valorHora = 100, markup = 2 });

        ClienteGraphQl.CodigosDeErro(negado).Should().Equal("AUTH_NOT_AUTHORIZED", "AUTH_NOT_AUTHORIZED");
        var precosNegados = negado["data"]!["projeto"]!["precos"]!;
        (precosNegados["cotacoes"], precosNegados["parametros"]).Should().Be((null, null), "sem precificacao.ler os valores não saem");
        precosNegados["herdadosEm"].Should().NotBeNull("a data da herança não é dado de preço");
        negado["data"]!["projeto"]!["nome"]!.GetValue<string>().Should().NotBeEmpty("o resto do projeto continua legível");
        lido["errors"].Should().BeNull();
        lido["data"]!["projeto"]!["precos"]!["cotacoes"]![0]!["valor"]!.GetValue<decimal>().Should().Be(640m);
        (edicaoSemRegra.StatusCode, edicaoDoLeitor.StatusCode).Should().Be((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden));
    }

    private async Task<ClienteGraphQl> EntrarAsync(UsuarioBuilder usuarios)
    {
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        return new ClienteGraphQl(_http, sessao.AccessToken);
    }

    private async Task<HttpResponseMessage> SendAsync(ClienteGraphQl cliente, HttpMethod metodo, string url, object? corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(url, UriKind.Relative)) { Content = corpo is null ? null : JsonContent.Create(corpo) };
        requisicao.Headers.Authorization = new("Bearer", cliente.Token);
        return await _http.SendAsync(requisicao);
    }

    private sealed record IdLido(long Id);
}
