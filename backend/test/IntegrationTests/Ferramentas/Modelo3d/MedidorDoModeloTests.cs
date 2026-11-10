using System.Text.Json;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;
using Castwork.DomainService.Precificacao;
using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Mcp;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Castwork.IntegrationTests.Ferramentas.Modelo3d;

/// <summary>
/// O medidor do modelo 3D com o SolidCraft de teste: um anel de ouro 18k com uma garra sobreposta, uma pedra e um corpo
/// sem material, medido sem IA (massa por liga, sobreposição) e guardado pelo arquivo. Precisa do <c>solidcraft-cli</c>
/// (<c>SolidCraft:Caminho</c> nos User Secrets da API ou <c>SolidCraft__Caminho</c>); sem ele, fica ignorado.
/// </summary>
public class MedidorDoModeloTests
{
    /// <summary>
    /// Aro (raio 5, altura 2) e garra (raio 2, encostada no aro) de ouro 18k amarelo, um brilhante ("Pedra") e um pino
    /// sem material, longe do anel.
    /// </summary>
    private const string Corpos = """
        {"commands":[
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[0,0],"radius":5}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Aro"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[4,0],"radius":2}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Garra","operation":"new"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[40,0],"radius":2}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Brilhante","operation":"new"}},
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[60,0],"radius":1}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Pino","operation":"new"}},
          {"command":"material.assign","params":{"bodies":["Aro","Garra"],"material":"Castwork liga 2"}},
          {"command":"material.assign","params":{"bodies":["Brilhante"],"material":"Pedra"}}
        ]}
        """;

    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    [Test]
    public async Task DeveMedirOAnelSemIaEGuardarAMedicaoPeloArquivoAsync()
    {
        var opcoes = new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() };
        var pasta = Directory.CreateTempSubdirectory("castwork-teste-medidor-");
        try
        {
            var caminho = await CriarAnelAsync(opcoes, pasta.FullName);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var medidor = new MedidorDoModeloSolidCraft(Options.Create(opcoes), cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);
            var leituras = 0;
            var arquivo = new ArquivoParaMedir(1, async ct =>
            {
                leituras++;
                return await File.ReadAllBytesAsync(caminho, ct);
            });

            var medicao = await medidor.MedirAsync(arquivo, Materiais(15.5m), CancellationToken.None);

            medicao.Should().NotBeNull();
            medicao!.Corpos.Select(c => (c.Nome, c.Material, c.Fechado)).Should().Equal(
                ("Aro", "Castwork liga 2", true), ("Garra", "Castwork liga 2", true), ("Brilhante", "Pedra", true), ("Pino", "Default", true));
            medicao.Corpos[0].MassaGramas.Should().BeApproximately(2.4347m, 0.0001m, "π × 5² × 2 = 157,08 mm³ × 15,5 g/cm³");
            medicao.Sobreposicoes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { CorpoA = "Aro", CorpoB = "Garra" });
            medicao.Sobreposicoes[0].VolumeMm3.Should().BeApproximately(19.398m, 0.01m);

            var modelo = PesoDoModelo.Classificar(medicao);
            modelo.PesoPorLiga().Should().ContainSingle("aro + garra; pedra e corpo sem material ficam fora").Which.Should().Be((2L, Peso.EmGramas(2.824m)));
            modelo.Corpos.Where(c => c.Uso == UsoDoCorpo.SemMaterial).Select(c => c.Nome).Should().Equal("Pino");
            modelo.SobreposicoesDeMetal().Should().ContainSingle();

            (await medidor.MedirAsync(arquivo, Materiais(15.5m), CancellationToken.None)).Should().BeSameAs(medicao);
            leituras.Should().Be(1, "o arquivo não mudou: a medição guardada vale");

            var comOutraDensidade = await medidor.MedirAsync(arquivo, Materiais(15.6m), CancellationToken.None);
            leituras.Should().Be(2, "a densidade da liga mudou no ateliê: mede de novo");
            comOutraDensidade!.Corpos[0].MassaGramas.Should().BeApproximately(2.4504m, 0.0001m);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    [Test]
    public async Task DeveDevolverNuloQuandoOArquivoNaoAbreAsync()
    {
        var opcoes = new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var medidor = new MedidorDoModeloSolidCraft(Options.Create(opcoes), cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);

        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(2, _ => Task.FromResult("não é um projeto"u8.ToArray())), Materiais(15.5m), CancellationToken.None);

        medicao.Should().BeNull();
        cache.Count.Should().Be(0, "a falha não é guardada");
    }

    private static IReadOnlyList<MaterialDoModelo> Materiais(decimal densidadeDoOuro18k)
        => MateriaisDoModelo.Montar([(2, "Ouro 18k amarelo", Metal.Ouro, densidadeDoOuro18k), (7, "Prata 925", Metal.Prata, 10.36m)]);

    /// <summary>
    /// O arquivo como a IA (ou o joalheiro) deixaria: com os materiais do ateliê e o material atribuído.
    /// </summary>
    private static async Task<string> CriarAnelAsync(SolidCraftOptions opcoes, string pasta)
    {
        var definicao = Modelo3dNaIa.Criar(opcoes);
        var caminho = Path.Combine(pasta, "anel.json");
        await using var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta, TempoLimite, CancellationToken.None);
        await ChamarAsync(mcp, definicao.NovoArquivo);
        (await PreparoDoArquivo.AplicarAsync(mcp, definicao, MateriaisDoModelo.ParaPreparo(Materiais(15.5m)), CancellationToken.None)).Should().BeNull();
        await ChamarAsync(mcp, ("batch", Corpos));
        await ChamarAsync(mcp, definicao.SalvarArquivo(caminho));
        return caminho;
    }

    private static async Task ChamarAsync(ClienteMcp mcp, (string Nome, string Argumentos) chamada)
    {
        var resultado = await mcp.ChamarFerramentaAsync(chamada.Nome, JsonDocument.Parse(chamada.Argumentos).RootElement, CancellationToken.None);
        resultado.Erro.Should().BeFalse(resultado.Texto);
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
}
