using System.Text.Json.Nodes;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// Os materiais do ateliê no arquivo 3D: os comandos do SolidCraft montados a partir das ligas (nomes estáveis, sem
/// duplicar, densidade atualizada) e as instruções da IA para atribuir material.
/// </summary>
public class MateriaisNoArquivoTests
{
    private const string SoOsDoSolidCraft = """{"materials":[{"density_g_cm3":1.29,"name":"Default"},{"density_g_cm3":7.85,"name":"Steel"}]}""";

    private static readonly IReadOnlyList<MaterialDoModelo> Materiais = MateriaisDoModelo.Montar(
    [
        (2, "Ouro 18k amarelo", Metal.Ouro, 15.5m),
        (3, "Ouro 18k branco", Metal.Ouro, 15m),
        (4, "Ouro 18k rosé", Metal.Ouro, 15.1m),
        (7, "Prata 925", Metal.Prata, 10.36m),
    ]);

    [Test]
    public void DeveMontarUmMaterialPorLigaComNomeEstavelMaisPedraEApoio()
    {
        Materiais.Select(m => (m.Nome, m.Densidade, m.Cor, m.LigaId)).Should().Equal(
            ("Castwork liga 2", 15.5m, "#d9b44a", 2L),
            ("Castwork liga 3", 15m, "#e4e1d6", 3L),
            ("Castwork liga 4", 15.1m, "#e2a98e", 4L),
            ("Castwork liga 7", 10.36m, "#c8cbd0", 7L),
            ("Pedra", MateriaisDoModelo.DensidadeDaPedra, "#cfe6f2", (long?)null),
            ("Apoio (não conta)", MateriaisDoModelo.DensidadeDoApoio, "#9aa3ad", (long?)null));
    }

    [Test]
    public void DeveManterONomeDoMaterialQuandoONomeDaLigaMuda()
    {
        var renomeada = MateriaisDoModelo.Montar([(2, "Ouro amarelo 750", Metal.Ouro, 15.5m)])[0];

        renomeada.Nome.Should().Be("Castwork liga 2");
        MateriaisDoModelo.LigaDoMaterial(renomeada.Nome).Should().Be(2);
        MateriaisDoModelo.LigaDoMaterial("Pedra").Should().BeNull();
        MateriaisDoModelo.LigaDoMaterial("Castwork liga x").Should().BeNull();
    }

    [Test]
    public void DeveCriarNoArquivoTodosOsMateriaisQueFaltam()
    {
        var comandos = MateriaisNoArquivo.Comandos(SoOsDoSolidCraft, Materiais);

        comandos.Select(c => (Texto(c, "command"), Texto(c["params"]!, "name"))).Should().Equal(
            ("material.create", "Castwork liga 2"),
            ("material.create", "Castwork liga 3"),
            ("material.create", "Castwork liga 4"),
            ("material.create", "Castwork liga 7"),
            ("material.create", "Pedra"),
            ("material.create", "Apoio (não conta)"));
        comandos[0]["params"]!.ToJsonString().Should().Be(
            """{"name":"Castwork liga 2","density_g_cm3":15.5,"color":"#d9b44a","pbr":{"metallic":1,"roughness":0.25,"dielectric_ior":1.5}}""");
        comandos[4]["params"]!.ToJsonString().Should().Be("""{"name":"Pedra","density_g_cm3":3.5,"color":"#cfe6f2","opacity":0.85}""");
    }

    [Test]
    public void DeveNaoFazerNadaQuandoOArquivoJaTemOsMateriais()
    {
        var lista = Lista(Materiais.Select(m => (m.Nome, (double)m.Densidade)));

        MateriaisNoArquivo.Comandos(lista, Materiais).Should().BeEmpty();
        MateriaisNoArquivo.Garantir(lista, MateriaisDoModelo.ParaPreparo(Materiais)).Should().BeNull("abrir de novo não duplica");
    }

    [Test]
    public void DeveAtualizarSoADensidadeQueMudouNoAtelie()
    {
        var lista = Lista(Materiais.Select(m => (m.Nome, m.Nome == "Castwork liga 3" ? 14.8 : (double)m.Densidade)));

        var comandos = MateriaisNoArquivo.Comandos(lista, Materiais);

        comandos.Should().ContainSingle().Which.ToJsonString().Should().Be(
            """{"command":"material.edit","params":{"material":"Castwork liga 3","density_g_cm3":15}}""");
    }

    [Test]
    public void DeveCriarSoOQueFaltaEMandarTudoNumBatch()
    {
        var lista = Lista(Materiais.Where(m => m.Nome != "Pedra").Select(m => (m.Nome, (double)m.Densidade)));

        var chamada = MateriaisNoArquivo.Garantir(lista, MateriaisDoModelo.ParaPreparo(Materiais));

        chamada.Should().NotBeNull();
        chamada!.Value.Nome.Should().Be("batch");
        JsonNode.Parse(chamada.Value.Argumentos)!["commands"]!.AsArray().Select(c => Texto(c!["params"]!, "name")).Should().Equal("Pedra");
    }

    [Test]
    public void DeveIgnorarUmaListaQueNaoPodeSerLida()
    {
        MateriaisNoArquivo.Garantir("Erro: sem projeto aberto.", MateriaisDoModelo.ParaPreparo(Materiais)).Should().BeNull();
    }

    [Test]
    public void DeveGarantirOsMateriaisAoAbrirOArquivoDoModelo3d()
    {
        var definicao = Modelo3dNaIa.Criar(new SolidCraftOptions { Caminho = "/solidcraft-cli" });

        definicao.PrepararArquivo.Should().NotBeNull();
        definicao.PrepararArquivo!.Value.Leitura.Should().Be(("execute", """{"command":"material.list"}"""));
    }

    [Test]
    public void DevePedirNasInstrucoesParaAtribuirMaterialAosCorposCriados()
    {
        Modelo3dNaIa.Instrucoes.Should().Contain("`material.assign`")
            .And.Contain("Todo corpo de metal que você criar recebe o material da")
            .And.Contain("\"Pedra\"")
            .And.Contain("\"Apoio (não conta)\"")
            .And.Contain("pergunte ao joalheiro")
            .And.Contain("\"materiais\" no contexto");
    }

    private static string Lista(IEnumerable<(string Nome, double Densidade)> doArquivo)
        => new JsonObject
        {
            ["materials"] = new JsonArray(
            [
                new JsonObject { ["name"] = "Default", ["density_g_cm3"] = 1.29 },
                .. doArquivo.Select(m => new JsonObject { ["name"] = m.Nome, ["density_g_cm3"] = m.Densidade, ["user"] = true }),
            ]),
        }.ToJsonString();

    private static string? Texto(JsonNode no, string campo) => no[campo]?.GetValue<string>();
}
