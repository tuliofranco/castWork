using System.Net;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Domain.Entities.Precificacao;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Orçamento da versão como a tela usa: escrita pelo REST (<c>api/projetos/{id}/versoes/{versaoId}/...</c>) e leitura no
/// campo <c>preco</c> da versão no GraphQL, com o cálculo protegido por <c>precificacao.ler</c>.
/// </summary>
public class PrecoDaVersaoEndpointsTests : TesteDeIntegracao
{
    private const string ConsultaCompleta = """
        query ($id: Long!) {
          projeto(id: $id) {
            versoes {
              id
              preco {
                preco
                status
                calculo {
                  custoTotal
                  markup
                  markupDaVersao
                  precoSemArredondar
                  metais { ligaId liga pesoEstimado pesoAFundir valorPorGrama custo pesoReal diferencaDoPesoReal }
                  composicao { pecaDoProjetoId quantidade custoUnitario custo }
                  maoDeObra { tipo horas valorHora custo }
                  servicos { nome valor }
                  pendencias { tipo descricao pecaDoProjetoId ligaId metal }
                  avisos { tipo descricao }
                }
              }
            }
          }
        }
        """;

    private const string ConsultaSoPreco = """
        query ($id: Long!, $versaoId: Long!) {
          projeto(id: $id) { versoes(id: $versaoId) { id preco { preco status } } }
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
    public async Task DeveMontarOOrcamentoPelaApiELerOPrecoPeloGraphQlAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var (projetoId, versaoId, pecaId) = await VersaoComUmaPecaAsync(custo: 400m);
        var rota = $"/api/projetos/{projetoId}/versoes/{versaoId}";

        var respostas = new[]
        {
            await SendAsync(admin, HttpMethod.Put, $"{rota}/metais", new { metais = new[] { new { ligaId = 2, pesoGramas = 5 } } }),
            await SendAsync(admin, HttpMethod.Put, $"{rota}/mao-de-obra", new { tipo = "PorHoras", horas = 6 }),
            await SendAsync(admin, HttpMethod.Put, $"{rota}/servicos", new { servicos = new[] { new { nome = "Fundição", valor = 120 }, new { nome = "Ródio", valor = 60 } } }),
            await SendAsync(admin, HttpMethod.Put, $"{rota}/markup", new { markup = (decimal?)null }),
            await SendAsync(admin, HttpMethod.Put, $"{rota}/metais/2/peso-real", new { pesoGramas = 5.2 }),
        };
        var recusado = await SendAsync(admin, HttpMethod.Put, $"{rota}/mao-de-obra", new { tipo = "ValorFixo" });
        var lido = await admin.ExecutarAsync(ConsultaCompleta, new { id = projetoId });

        respostas.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.NoContent);
        recusado.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        lido["errors"].Should().BeNull();
        var preco = lido["data"]!["projeto"]!["versoes"]![0]!["preco"]!;
        (preco["preco"]!.GetValue<decimal>(), preco["status"]!.GetValue<string>()).Should().Be((7850m, "COMPLETO"));
        var calculo = preco["calculo"]!;
        (calculo["custoTotal"]!.GetValue<decimal>(), calculo["precoSemArredondar"]!.GetValue<decimal>(), calculo["markupDaVersao"]).Should().Be((4112m, 7845.70m, null));
        var ouro = calculo["metais"]![0]!;
        (ouro["liga"]!.GetValue<string>(), ouro["pesoAFundir"]!.GetValue<decimal>(), ouro["valorPorGrama"]!.GetValue<decimal>(), ouro["custo"]!.GetValue<decimal>())
            .Should().Be(("Ouro 18k amarelo", 5.4m, 480m, 2592m));
        (ouro["pesoReal"]!.GetValue<decimal>(), ouro["diferencaDoPesoReal"]!.GetValue<decimal>()).Should().Be((5.2m, 4m));
        (calculo["composicao"]![0]!["pecaDoProjetoId"]!.GetValue<long>(), calculo["composicao"]![0]!["custo"]!.GetValue<decimal>()).Should().Be((pecaId, 800m));
        (calculo["maoDeObra"]!["tipo"]!.GetValue<string>(), calculo["maoDeObra"]!["custo"]!.GetValue<decimal>()).Should().Be(("POR_HORAS", 540m));
        calculo["servicos"]!.AsArray().Select(s => s!["nome"]!.GetValue<string>()).Should().Equal("Fundição", "Ródio");
        calculo["pendencias"]!.AsArray().Should().BeEmpty();
    }

    [Test]
    public async Task DeveMostrarSoOPrecoEOStatusSemPrecificacaoLerAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var (projetoId, versaoId, _) = await VersaoComUmaPecaAsync(custo: null);
        var soProjetos = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar, Regras.Projetos.Editar).BuildAsync();
        var semPrecificacao = await EntrarAsync(Usar(new UsuarioBuilder()).DoPerfil(soProjetos));
        await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Outra" });

        var soPreco = await semPrecificacao.ExecutarAsync(ConsultaSoPreco, new { id = projetoId, versaoId });
        var negado = await semPrecificacao.ExecutarAsync(ConsultaCompleta, new { id = projetoId });
        var completo = await admin.ExecutarAsync(ConsultaCompleta, new { id = projetoId });
        var edicao = await SendAsync(semPrecificacao, HttpMethod.Put, $"/api/projetos/{projetoId}/versoes/{versaoId}/metais", new { metais = Array.Empty<object>() });

        soPreco["errors"].Should().BeNull();
        var versaoPedida = soPreco["data"]!["projeto"]!["versoes"]!.AsArray().Should().ContainSingle("o argumento id filtra a versão").Subject!;
        (versaoPedida["id"]!.GetValue<long>(), versaoPedida["preco"]!["status"]!.GetValue<string>()).Should().Be((versaoId, "INCOMPLETO"));
        ClienteGraphQl.CodigosDeErro(negado).Should().Equal("AUTH_NOT_AUTHORIZED", "AUTH_NOT_AUTHORIZED");
        var precoNegado = negado["data"]!["projeto"]!["versoes"]![1]!["preco"]!;
        precoNegado["calculo"].Should().BeNull("sem precificacao.ler os custos, o markup e as cotações não saem");
        precoNegado["status"]!.GetValue<string>().Should().Be("INCOMPLETO");
        completo["data"]!["projeto"]!["versoes"]![1]!["preco"]!["calculo"]!["pendencias"]!.AsArray().Select(p => p!["tipo"]!.GetValue<string>())
            .Should().BeEquivalentTo("SEM_METAL_COM_PESO", "PECA_SEM_VALOR");
        edicao.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeveRecusarOrcamentoDaVersaoPublicadaMasAceitarOPesoRealAsync()
    {
        var admin = await EntrarAsync(Usar(new UsuarioBuilder()).Admin());
        var (projetoId, versaoId, _) = await VersaoComUmaPecaAsync(custo: 400m);
        var rota = $"/api/projetos/{projetoId}/versoes/{versaoId}";
        await SendAsync(admin, HttpMethod.Put, $"{rota}/metais", new { metais = new[] { new { ligaId = 2, pesoGramas = 5 } } });
        await AmbienteDeTeste.SendAsync(new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId });

        var metais = await SendAsync(admin, HttpMethod.Put, $"{rota}/metais", new { metais = new[] { new { ligaId = 2, pesoGramas = 6 } } });
        var markup = await SendAsync(admin, HttpMethod.Put, $"{rota}/markup", new { markup = 2 });
        var pesoReal = await SendAsync(admin, HttpMethod.Put, $"{rota}/metais/2/peso-real", new { pesoGramas = 4.9 });
        var ligaForaDaVersao = await SendAsync(admin, HttpMethod.Put, $"{rota}/metais/3/peso-real", new { pesoGramas = 1 });

        (metais.StatusCode, markup.StatusCode, pesoReal.StatusCode, ligaForaDaVersao.StatusCode)
            .Should().Be((HttpStatusCode.UnprocessableEntity, HttpStatusCode.UnprocessableEntity, HttpStatusCode.NoContent, HttpStatusCode.NotFound));
    }

    private async Task<(long ProjetoId, long VersaoId, long PecaDoProjetoId)> VersaoComUmaPecaAsync(decimal? custo)
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 8m, ValorHora = 90m, Markup = 1.8m, TaxasPercentual = 6m, ArredondarPara = 10m });
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var pecaId = await Usar(new PecaDoProjetoBuilder(projetoId).ComValor(custo).Publicada()).CriarAsync();
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaDoProjetoId = pecaId, Quantidade = 2 });
        return (projetoId, versaoId, pecaId);
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
}
