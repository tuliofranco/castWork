using System.Text.Json.Nodes;

using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// As chamadas do SolidCraft da conferência de peso e materiais: <c>material.assign</c> agrupado por material (num
/// <c>batch</c>, tudo ou nada) e <c>solid.combine</c> com <c>join</c>, o primeiro corpo como alvo.
/// </summary>
public class EdicaoNoArquivoTests
{
    [Test]
    public void DeveAtribuirOsMateriaisNumBatchComUmComandoPorMaterial()
    {
        var (nome, argumentos) = EdicaoNoArquivo.Chamada(new DefinicaoDeMateriais(
        [
            new MaterialDoCorpo("Aro", "Castwork liga 2"),
            new MaterialDoCorpo("Brilhante", "Pedra"),
            new MaterialDoCorpo("Chaton", "Castwork liga 2"),
        ]));

        nome.Should().Be("batch");
        var comandos = JsonNode.Parse(argumentos)!["commands"]!.AsArray();
        comandos.Select(c => c!.ToJsonString()).Should().Equal(
            """{"command":"material.assign","params":{"bodies":["Aro","Chaton"],"material":"Castwork liga 2"}}""",
            """{"command":"material.assign","params":{"bodies":["Brilhante"],"material":"Pedra"}}""");
    }

    [Test]
    public void DeveUnirOsCorposAoPrimeiro()
    {
        var (nome, argumentos) = EdicaoNoArquivo.Chamada(new UniaoDeCorpos("Aro", ["Chaton", "Garra"]));

        nome.Should().Be("execute");
        JsonNode.Parse(argumentos)!.ToJsonString().Should().Be(
            """{"command":"solid.combine","params":{"target":"Aro","tools":["Chaton","Garra"],"operation":"join"}}""");
    }

    [Test]
    public void DeveDescreverAAlteracaoEmPortugues()
    {
        EdicaoNoArquivo.Descricao(new UniaoDeCorpos("Aro", ["Chaton"])).Should().Be("unir os corpos");
        EdicaoNoArquivo.Descricao(new DefinicaoDeMateriais([])).Should().Be("definir o material dos corpos");
    }

    [Test]
    public void DeveRenomearOsCorposNumBatchNaOrdem()
    {
        var (nome, argumentos) = EdicaoNoArquivo.Chamada(new RenomeacaoDeCorpos([("Corpo1", "Elo"), ("Corpo2", "Elo (2)")]));

        nome.Should().Be("batch");
        argumentos.Should().Be(
            """{"commands":[{"command":"body.rename","params":{"body":"Corpo1","name":"Elo"}},{"command":"body.rename","params":{"body":"Corpo2","name":"Elo (2)"}}]}""");
    }
}
