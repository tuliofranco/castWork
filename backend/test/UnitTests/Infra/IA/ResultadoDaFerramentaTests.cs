using Castwork.Infra.IA.Assistente.Conversa;
using Castwork.Infra.IA.Ferramentas.Desenho2d;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// O que volta do MCP antes de ir para a IA: o JSON compactado e o resumo do modelo depois de uma alteração.
/// </summary>
public class ResultadoDaFerramentaTests
{
    private const string Medida = """
        {
          "bodies": [
            {
              "name": "Aro",
              "closed": true,
              "bbox": { "min": [-10.0, -10.0, -1.0409497792752501e-15], "max": [10.0, 10.0, 3.000000000000001] },
              "volume_mm3": 261.5375884066989,
              "face_types": { "cylinder": 2, "plane": 2 },
              "kernel": { "edges": 6 },
              "notes": []
            }
          ]
        }
        """;

    [Test]
    public void DeveCompactarOJsonSemEspacosComNumerosCurtosESemCamposInternosOuVazios()
    {
        var compacto = ResultadoCompacto.Compactar(Medida, new HashSet<string> { "kernel" });

        compacto.Should().Be("""{"bodies":[{"name":"Aro","closed":true,"bbox":{"min":[-10,-10,0],"max":[10,10,3]},"volume_mm3":261.5376,"face_types":{"cylinder":2,"plane":2}}]}""");
    }

    [Test]
    public void DeveDeixarComoVeioOTextoQueNaoEJson()
    {
        ResultadoCompacto.Compactar("Ferramenta desconhecida: x.").Should().Be("Ferramenta desconhecida: x.");
    }

    [Test]
    public void DeveResumirCadaCorpoEmUmaLinha()
    {
        ResumoDoModelo.Resumir(Medida).Should().Be("Modelo agora:\n- Aro (fechado): 20 × 20 × 3 mm · volume 261,54 mm³ · faces: 2 cylinder, 2 plane");
    }

    [Test]
    public void DeveDizerQuandoNaoHaCorpo()
    {
        ResumoDoModelo.Resumir("""{"bodies":[]}""").Should().Be("Modelo agora: nenhum corpo sólido.");
    }

    [Test]
    public void DeveResumirODesenhoEmUmaLinha()
    {
        const string Inspecao = """
            {"counts":{"Circle":1,"Line":1},"extents":[[-12.0,-10.0],[12.0,10.0]],
             "layers":[{"name":"0","on":true},{"name":"Cotas","on":true}]}
            """;

        ResumoDoDesenho.Resumir(Inspecao).Should().Be("Desenho agora: 1 Circle, 1 Line · extensão 24 × 20 mm · camadas: 0, Cotas");
    }
}
