using System.Text.Json;

using Castwork.Application.Assistente;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Application.Precificacao.Commands.AlterarLiga;
using Castwork.Application.Precificacao.Commands.DesativarLiga;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Mcp;
using Castwork.Infra.IA.Ferramentas.Modelo3d;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.Ferramentas.Modelo3d;

/// <summary>
/// Os materiais do ateliê no arquivo 3D: o que o turno do Modelo 3D leva (contexto e preparo do arquivo) e, com o
/// SolidCraft de teste, os materiais criados no arquivo e lidos de volta pelo <c>measure</c>. Os testes com o SolidCraft
/// precisam do <c>solidcraft-cli</c> (<c>SolidCraft:Caminho</c> nos User Secrets da API ou <c>SolidCraft__Caminho</c>);
/// sem ele, ficam ignorados.
/// </summary>
public class MateriaisDoModelo3dTests : TesteDeIntegracao
{
    /// <summary>
    /// Três corpos: o aro com a liga, uma garra sem material (o Default do SolidCraft) e a pedra.
    /// </summary>
    private const string Corpos = """
        {"commands":[
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[0,0],"radius":5}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Aro"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[20,0],"radius":1}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Garra","operation":"new"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[40,0],"radius":2}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Brilhante","operation":"new"}},
          {"command":"material.assign","params":{"bodies":["Aro"],"material":"Castwork liga 2"}},
          {"command":"material.assign","params":{"bodies":["Brilhante"],"material":"Pedra"}}
        ]}
        """;

    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    [Test]
    public async Task DeveLevarAoTurnoOsMateriaisDasLigasAtivasMaisPedraEApoioAsync()
    {
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = 1 });

        var (contexto, _, preparo) = await PrepararTurnoAsync();

        var materiais = MateriaisDoModelo.LerPreparo(preparo!);
        materiais.Select(m => m.Nome).Should().Equal(
            "Castwork liga 2", "Castwork liga 3", "Castwork liga 4", "Castwork liga 5", "Castwork liga 6", "Castwork liga 7", "Pedra", "Apoio (não conta)");
        materiais[0].Should().BeEquivalentTo(new { LigaId = 2L, Liga = "Ouro 18k amarelo", Densidade = 15.5m });

        using var json = JsonDocument.Parse(contexto);
        var doContexto = json.RootElement.GetProperty("materiais").EnumerateArray().ToList();
        doContexto.Select(m => m.GetProperty("nome").GetString()).Should().Equal(materiais.Select(m => m.Nome), "a IA conhece os mesmos materiais do arquivo");
        doContexto[0].GetProperty("uso").GetString().Should().Be("metal: corpo de Ouro 18k amarelo");
    }

    [Test]
    public async Task DeveCriarOsMateriaisNoArquivoSemDuplicarELerDeVoltaPeloMeasureAsync()
    {
        var definicao = Modelo3dNaIa.Criar(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var (_, _, preparo) = await PrepararTurnoAsync();
        var pasta = Directory.CreateTempSubdirectory("castwork-teste-3d-");
        try
        {
            await using (var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, CancellationToken.None))
            {
                await ChamarAsync(mcp, definicao.NovoArquivo);

                (await PreparoDoArquivo.AplicarAsync(mcp, definicao, preparo, CancellationToken.None)).Should().BeNull();
                (await PreparoDoArquivo.AplicarAsync(mcp, definicao, preparo, CancellationToken.None)).Should().BeNull("abrir de novo não muda nada");
                (await MateriaisDoArquivoAsync(mcp)).Should().Equal(
                    ("Castwork liga 1", 19.3),
                    ("Castwork liga 2", 15.5),
                    ("Castwork liga 3", 15.0),
                    ("Castwork liga 4", 15.1),
                    ("Castwork liga 5", 13.4),
                    ("Castwork liga 6", 10.4),
                    ("Castwork liga 7", 10.36),
                    ("Pedra", 3.5),
                    ("Apoio (não conta)", 1.0));

                await ChamarAsync(mcp, ("batch", Corpos));
                await ChamarAsync(mcp, definicao.SalvarArquivo(Path.Combine(pasta.FullName, "modelo.json")));
            }

            // O arquivo salvo guarda os materiais e a atribuição; a liga mudou de densidade no ateliê.
            await AmbienteDeTeste.SendAsync(new AlterarLigaCommand { LigaId = 2, Nome = "Ouro amarelo 750", Metal = Metal.Ouro, Teor = "750", Densidade = 15.6m });
            var (_, _, novoPreparo) = await PrepararTurnoAsync();
            await using (var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, CancellationToken.None))
            {
                await ChamarAsync(mcp, definicao.AbrirArquivo(Path.Combine(pasta.FullName, "modelo.json")));
                (await PreparoDoArquivo.AplicarAsync(mcp, definicao, novoPreparo, CancellationToken.None)).Should().BeNull();

                (await MateriaisDoArquivoAsync(mcp)).Should().HaveCount(9, "nenhum material duplicado")
                    .And.Contain(("Castwork liga 2", 15.6), "o nome é estável e a densidade acompanha o ateliê");

                var medida = await ChamarAsync(mcp, definicao.ResumoDoArquivo!.Value.Chamada);
                using var json = JsonDocument.Parse(medida);
                var corpos = json.RootElement.GetProperty("bodies").EnumerateArray()
                    .Select(c => (c.GetProperty("name").GetString(), c.GetProperty("material").GetString(), c.GetProperty("density_g_cm3").GetDouble()))
                    .ToList();
                corpos.Should().Equal(("Aro", "Castwork liga 2", 15.6), ("Garra", "Default", 1.29), ("Brilhante", "Pedra", 3.5));
                definicao.ResumoDoArquivo.Value.Resumir(medida).Should()
                    .Contain("Aro (fechado, Castwork liga 2)")
                    .And.Contain("Garra (fechado, SEM MATERIAL)")
                    .And.EndWith("Sem material (atribua com material.assign): Garra.");
            }
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    private static async Task<List<(string? Nome, double Densidade)>> MateriaisDoArquivoAsync(ClienteMcp mcp)
    {
        using var json = JsonDocument.Parse(await ChamarAsync(mcp, MateriaisNoArquivo.Leitura));
        return [.. json.RootElement.GetProperty("materials").EnumerateArray()
            .Where(m => m.TryGetProperty("user", out var u) && u.GetBoolean())
            .Select(m => (m.GetProperty("name").GetString(), m.GetProperty("density_g_cm3").GetDouble()))];
    }

    private static async Task<string> ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, CancellationToken.None);
        resultado.Erro.Should().BeFalse(resultado.Texto);
        return resultado.Texto;
    }

    private static string CaminhoDoSolidCraft()
    {
        var caminho = new ConfigurationBuilder()
            .AddUserSecrets("castwork-webapi")
            .AddEnvironmentVariables()
            .Build()[$"{SolidCraftOptions.Secao}:Caminho"];
        if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
        {
            Assert.Ignore("SolidCraft não configurado (SolidCraft:Caminho): teste com o SolidCraft ignorado.");
        }

        return caminho!;
    }

    private async Task<PreparoDoTurno> PrepararTurnoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        return await AmbienteDeTeste.ExecutarNoEscopoAsync(sp => sp.GetServices<IFerramentaComIa>()
            .Single(f => f.Codigo == Modelo3dNoAssistente.Codigo)
            .PrepararAsync(projetoId, versaoId, CancellationToken.None));
    }
}
