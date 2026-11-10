using System.Text.Json.Nodes;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// As chamadas do SolidCraft que montam o modelo da versão a partir das peças (<c>component.insert_design</c> deslocado em
/// X e <c>component.break_link</c>), a leitura do que ele devolve e o acerto dos materiais trazidos.
/// </summary>
public class MontagemNoArquivoTests
{
    private static readonly IReadOnlyList<MaterialDoModelo> Materiais = MateriaisDoModelo.Montar([(2, "Ouro 18k amarelo", Metal.Ouro, 15.5m)]);

    [Test]
    public void DeveInserirOArquivoDaPecaDeslocadoEmXEDepoisSoltarOComponente()
    {
        var inserir = MontagemNoArquivo.Inserir("peca-0.json", "Elo oval 2", 17.5m);
        var desligar = MontagemNoArquivo.Desligar(4);

        inserir.Nome.Should().Be("execute");
        JsonNode.Parse(inserir.Argumentos)!.ToJsonString().Should().Be(
            """{"command":"component.insert_design","params":{"path":"peca-0.json","name":"Elo oval 2","translate":[17.5,0,0]}}""");
        JsonNode.Parse(desligar.Argumentos)!.ToJsonString().Should().Be("""{"command":"component.break_link","params":{"component":4}}""");
    }

    [Test]
    public void DeveLerOComponenteEOsCorposDaInsercao()
    {
        var insercao = MontagemNoArquivo.LerInsercao("""{"bodies":["Aro (2)"],"component":4,"created_bodies":["Aro (2)","Brilhante (2)"],"name":"Aro 2"}""");

        insercao.Should().NotBeNull();
        insercao!.Value.Componente.Should().Be(4);
        insercao.Value.Corpos.Should().Equal("Aro (2)", "Brilhante (2)");
        MontagemNoArquivo.LerInsercao("""{"erro":"x"}""").Should().BeNull();
        MontagemNoArquivo.LerInsercao("não é json").Should().BeNull();
    }

    [Test]
    public void DeveAcharAFaixaEmXDeTodosOsCorpos()
    {
        var faixa = MontagemNoArquivo.FaixaEmX("""
            {"bodies":[
              {"name":"Aro","bbox":{"min":[-5,-5,0],"max":[5,5,2]}},
              {"name":"Brilhante","bbox":{"min":[19,-1,0],"max":[21,1,2]}},
              {"name":"Sem caixa"}
            ]}
            """);

        faixa.Should().Be((-5m, 21m));
        MontagemNoArquivo.FaixaEmX("""{"bodies":[]}""").Should().BeNull("modelo vazio: a fila começa na origem");
    }

    [Test]
    public void DeveAcertarOMaterialRenomeadoEDarALigaDaPecaAoCorpoSemMaterial()
    {
        var medida = """
            {"bodies":[
              {"name":"Base","material":"Default"},
              {"name":"Aro","material":"Castwork liga 2 (linked 1)"},
              {"name":"Garra","material":"Default"},
              {"name":"Brilhante","material":"Pedra"},
              {"name":"Fecho","material":"Default"}
            ]}
            """;
        var ligaDoCorpo = new Dictionary<string, string> { ["Aro"] = "Castwork liga 2", ["Garra"] = "Castwork liga 2", ["Brilhante"] = "Castwork liga 2", ["Fecho"] = string.Empty };

        var acerto = MontagemNoArquivo.AcertarMateriais(medida, ligaDoCorpo, Materiais);

        acerto.Should().NotBeNull();
        acerto!.Value.Nome.Should().Be("batch");
        JsonNode.Parse(acerto.Value.Argumentos)!["commands"]!.AsArray().Select(c => c!.ToJsonString()).Should().Equal(
            """{"command":"material.assign","params":{"bodies":["Aro","Garra"],"material":"Castwork liga 2"}}""");
    }

    [Test]
    public void DeveDeixarOsMateriaisComoEstaoQuandoNaoHaOQueAcertar()
        => MontagemNoArquivo.AcertarMateriais("""{"bodies":[{"name":"Aro","material":"Castwork liga 2"}]}""", new Dictionary<string, string> { ["Aro"] = "Castwork liga 2" }, Materiais)
            .Should().BeNull();

    [Test]
    public void DeveNomearCadaUnidadePelaPeca()
        => NomesDosCorpos.DaUnidade("Elo Infinito 2,5 cm", 3).Should().Be("Elo Infinito 2,5 cm (3)");

    [Test]
    public void DeveDarAosCorposDaUnidadeONomeDela()
    {
        var (chamada, nomes) = MontagemNoArquivo.RenomearCorpos(["Elo", "Elo (2)"], "Elo (1)");

        nomes.Should().Equal("Elo (1)", "Elo (1) (2)");
        chamada!.Value.Nome.Should().Be("batch");
        chamada.Value.Argumentos.Should().Be(
            """{"commands":[{"command":"body.rename","params":{"body":"Elo","name":"Elo (1)"}},{"command":"body.rename","params":{"body":"Elo (2)","name":"Elo (1) (2)"}}]}""");
    }

    [Test]
    public void DeveDeixarOsCorposDaUnidadeComoEstaoQuandoJaTemONome()
        => MontagemNoArquivo.RenomearCorpos(["Elo (1)"], "Elo (1)").Chamada.Should().BeNull();
}
