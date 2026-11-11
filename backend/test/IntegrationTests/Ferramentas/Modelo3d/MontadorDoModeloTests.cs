using System.Globalization;
using System.Text.Json;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Infra.IA.Assistente.Mcp;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Castwork.IntegrationTests.Ferramentas.Modelo3d;

/// <summary>
/// "Montar a partir das peças" com o SolidCraft de teste, sem IA: o modelo da versão (com uma base) recebe duas unidades
/// do elo (feito quando o ouro 18k tinha outra densidade, com uma solda sem material) e uma da pedra, enfileiradas sem se
/// sobrepor; os corpos ficam com os materiais do ateliê. Precisa do <c>solidcraft-cli</c> (<c>SolidCraft:Caminho</c> nos
/// User Secrets da API ou <c>SolidCraft__Caminho</c>); sem ele, fica ignorado.
/// </summary>
public class MontadorDoModeloTests
{
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    [Test]
    public async Task DeveTrazerUmaCopiaDaPecaPorUnidadeComOMaterialDoAtelieAsync()
    {
        var opcoes = Options.Create(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var montador = new MontadorDoModeloSolidCraft(opcoes, NullLogger<MontadorDoModeloSolidCraft>.Instance);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var medidor = new MedidorDoModeloSolidCraft(opcoes, cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);

        // O elo foi modelado quando o ouro 18k tinha 15,4 g/cm³: no arquivo dele, o material tem a densidade antiga.
        var elo = await CriarAsync(opcoes.Value, Cilindro("Elo", 0, 2), Cilindro("Solda", 2.5m, 0.5m), Material("Castwork liga 2", 15.4m, ["Elo"]));
        var pedra = await CriarAsync(opcoes.Value, Cilindro("Brilhante", 0, 1), Material(MateriaisDoModelo.Pedra, MateriaisDoModelo.DensidadeDaPedra, ["Brilhante"]));
        var versao = await CriarAsync(opcoes.Value, Cilindro("Base", 0, 3));

        var montado = await montador.MontarAsync(
            versao,
            Materiais(),
            [new PecaParaMontar("Elo oval", elo, 2, "Castwork liga 2"), new PecaParaMontar("Brilhante", pedra, 1, null)],
            CancellationToken.None);
        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(1, _ => Task.FromResult(montado)), Materiais(), CancellationToken.None);

        medicao.Should().NotBeNull();
        medicao!.Corpos.Select(c => (c.Nome, c.Material)).Should().BeEquivalentTo(
        [
            ("Base", MateriaisDoModelo.SemMaterial),
            ("Elo oval (1)", "Castwork liga 2"),
            ("Elo oval (1) (2)", "Castwork liga 2"),
            ("Elo oval (2)", "Castwork liga 2"),
            ("Elo oval (2) (2)", "Castwork liga 2"),
            ("Brilhante (1)", MateriaisDoModelo.Pedra),
        ]);
        medicao.Sobreposicoes.Should().BeEmpty("as unidades ficam enfileiradas, sem encostar na base nem uma na outra");
        medicao.Corpos.Single(c => c.Nome == "Elo oval (2)").MassaGramas.Should().BeApproximately(0.1948m, 0.0001m, "π × 2² × 1 mm³ na densidade de hoje (15,5)");
    }

    [Test]
    public async Task DeveRecusarModeloDePecaQueNaoAbreAsync()
    {
        var opcoes = Options.Create(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var montador = new MontadorDoModeloSolidCraft(opcoes, NullLogger<MontadorDoModeloSolidCraft>.Instance);

        await FluentActions.Awaiting(() => montador.MontarAsync(null, Materiais(), [new PecaParaMontar("Elo oval", "não é um modelo"u8.ToArray(), 1, null)], CancellationToken.None))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("O modelo 3D da peça Elo oval não abre no SolidCraft.");
    }

    [Test]
    public async Task DeveDarAosCorposDoArquivoFinalONomeDaPecaAsync()
    {
        var opcoes = Options.Create(new SolidCraftOptions { Caminho = CaminhoDoSolidCraft() });
        var editor = new EditorDoModeloSolidCraft(opcoes, NullLogger<EditorDoModeloSolidCraft>.Instance);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var medidor = new MedidorDoModeloSolidCraft(opcoes, cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);
        var elo = await CriarAsync(opcoes.Value, Cilindro("Elo", 0, 2), Cilindro("Solda", 2.5m, 0.5m));

        var renomeado = await editor.AlterarAsync(
            elo,
            Materiais(),
            new RenomeacaoDeCorpos(NomesDosCorpos.Renomeacoes(["Elo", "Solda"], "Elo Infinito 2,5 cm")),
            CancellationToken.None);
        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(2, _ => Task.FromResult(renomeado)), Materiais(), CancellationToken.None);

        medicao!.Corpos.Select(c => c.Nome).Should().Equal("Elo Infinito 2,5 cm", "Elo Infinito 2,5 cm (2)");
    }

    private static IReadOnlyList<MaterialDoModelo> Materiais()
        => MateriaisDoModelo.Montar([(2, "Ouro 18k amarelo", Metal.Ouro, 15.5m)]);

    private static string Cilindro(string nome, decimal x, decimal raio) => $$$"""
        {"command":"sketch.create","params":{"plane":"XY"}},
        {"command":"sketch.circle.center","params":{"center":[{{{x.ToString(CultureInfo.InvariantCulture)}}},0],"radius":{{{raio.ToString(CultureInfo.InvariantCulture)}}}}},
        {"command":"sketch.finish","params":{}},
        {"command":"solid.extrude","params":{"distance":"1","body_name":"{{{nome}}}","operation":"new"}}
        """;

    private static string Material(string nome, decimal densidade, string[] corpos) => $$$"""
        {"command":"material.create","params":{"name":"{{{nome}}}","density_g_cm3":{{{densidade.ToString(CultureInfo.InvariantCulture)}}},"color":"#d9b44a"}},
        {"command":"material.assign","params":{"bodies":{{{JsonSerializer.Serialize(corpos)}}},"material":"{{{nome}}}"}}
        """;

    private static async Task<byte[]> CriarAsync(SolidCraftOptions opcoes, params string[] comandos)
    {
        var definicao = Modelo3dNaIa.Criar(opcoes);
        var pasta = Directory.CreateTempSubdirectory("castwork-teste-montador-");
        try
        {
            var caminho = Path.Combine(pasta.FullName, "modelo.json");
            await using (var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, CancellationToken.None))
            {
                await ChamarAsync(mcp, definicao.NovoArquivo);
                await ChamarAsync(mcp, ("batch", $$"""{"commands":[{{string.Join(",", comandos)}}]}"""));
                await ChamarAsync(mcp, definicao.SalvarArquivo(caminho));
            }

            return await File.ReadAllBytesAsync(caminho);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
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
