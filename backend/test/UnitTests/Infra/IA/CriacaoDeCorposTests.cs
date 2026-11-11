using System.Text.Json;

using Castwork.Infra.IA.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// No modelo 3D da versão, a IA só monta e posiciona: os comandos que criam corpos são recusados no <c>execute</c> e em
/// qualquer comando do <c>batch</c>.
/// </summary>
public class CriacaoDeCorposTests
{
    [TestCase("""{"command":"solid.extrude","params":{}}""", true)]
    [TestCase("""{"command":"solid.move","params":{}}""", false)]
    [TestCase("""{"command":"component.insert_design","params":{}}""", true)]
    public void DeveRecusarNoExecuteSoOQueCriaCorpos(string entrada, bool recusa)
        => (CriacaoDeCorpos.Recusar("execute", Entrada(entrada)) is not null).Should().Be(recusa);

    [Test]
    public void DeveRecusarOBatchInteiroQuandoUmComandoCriaCorpos()
    {
        CriacaoDeCorpos.Recusar("batch", Entrada("""{"commands":[{"command":"solid.move"},{"command":"solid.box"}]}""")).Should().Be(CriacaoDeCorpos.Motivo);
        CriacaoDeCorpos.Recusar("batch", Entrada("""{"commands":[{"command":"solid.move"},{"command":"solid.align"}]}""")).Should().BeNull();
    }

    [Test]
    public void DeveDeixarPassarAsOutrasFerramentas()
        => CriacaoDeCorpos.Recusar("measure", Entrada("{}")).Should().BeNull();

    private static Dictionary<string, JsonElement> Entrada(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
}
