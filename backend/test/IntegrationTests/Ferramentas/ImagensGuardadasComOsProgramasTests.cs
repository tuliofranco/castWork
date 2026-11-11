using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Mcp;
using Castwork.Infra.IA.Ferramentas.Desenho2d;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Castwork.IntegrationTests.Ferramentas;

/// <summary>
/// As imagens guardadas com os programas de verdade, sem IA: as quatro vistas do modelo 3D em 1920x1200 pelo SolidCraft e
/// o SVG e o PNG de 2400x1500 do desenho 2D pelo CADCraft. Precisam do <c>solidcraft-cli</c> e do <c>cadcraft-cli</c>
/// (<c>SolidCraft:Caminho</c> e <c>CadCraft:Caminho</c> nos User Secrets da API ou nas variáveis de ambiente); sem eles,
/// ficam ignorados.
/// </summary>
public class ImagensGuardadasComOsProgramasTests
{
    private const string Disco = """
        {"commands":[
          {"command":"sketch.create","params":{"plane":"XY"}},
          {"command":"sketch.circle.center","params":{"center":[0,0],"radius":10}},
          {"command":"sketch.finish","params":{}},
          {"command":"solid.extrude","params":{"distance":"2","body_name":"Aro"}}
        ]}
        """;

    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

    [Test]
    public async Task DeveGerarAsQuatroVistasDoModelo3dAsync()
    {
        var modelo = new Modelo3dNaIa(Options.Create(new SolidCraftOptions { Caminho = Caminho(SolidCraftOptions.Secao) }));
        var arquivo = await CriarAsync(modelo.Definicao, "anel.json", ("batch", Disco));

        var imagens = await new GeradorDeImagensMcp([modelo], NullLogger<GeradorDeImagensMcp>.Instance).GerarAsync(Modelo3dNaIa.Codigo, arquivo, CancellationToken.None);

        imagens.Select(i => i.Vista).Should().Equal("iso", "frente", "topo", "lado");
        imagens.Should().AllSatisfy(i => TamanhoDoPng(i.Conteudo).Should().Be((1920, 1200)));
        imagens.Select(i => Convert.ToHexString(i.Conteudo)).Should().OnlyHaveUniqueItems("cada vista olha de um lado");
    }

    [Test]
    public async Task DeveGerarOSvgEOPngDoDesenho2dAsync()
    {
        var desenho = new Desenho2dNaIa(Options.Create(new CadCraftOptions { Caminho = Caminho(CadCraftOptions.Secao) }));
        var arquivo = await CriarAsync(desenho.Definicao, "elo.dxf", ("command_line", """{"text":"circle 0,0 10"}"""));

        var imagens = await new GeradorDeImagensMcp([desenho], NullLogger<GeradorDeImagensMcp>.Instance).GerarAsync(Desenho2dNaIa.Codigo, arquivo, CancellationToken.None);

        imagens.Select(i => i.Vista).Should().Equal("2d", "2d");
        Encoding.UTF8.GetString(imagens[0].Conteudo).Should().Contain("<svg");
        TamanhoDoPng(imagens[1].Conteudo).Should().Be((2400, 1500));
    }

    /// <summary>
    /// Largura e altura do cabeçalho IHDR, logo depois da assinatura.
    /// </summary>
    private static (int Largura, int Altura) TamanhoDoPng(byte[] png)
    {
        png.AsSpan(0, 4).ToArray().Should().Equal(0x89, 0x50, 0x4E, 0x47);
        return (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4)));
    }

    private static async Task<byte[]> CriarAsync(DefinicaoDeFerramenta definicao, string nome, (string Nome, string Argumentos) desenhar)
    {
        var pasta = Directory.CreateTempSubdirectory("castwork-teste-imagens-");
        try
        {
            var caminho = Path.Combine(pasta.FullName, nome);
            await using (var mcp = await ClienteMcp.IniciarAsync(definicao.Executavel!, definicao.Argumentos, pasta.FullName, TempoLimite, CancellationToken.None))
            {
                foreach (var (ferramenta, argumentos) in new[] { definicao.NovoArquivo, desenhar, definicao.SalvarArquivo(caminho) })
                {
                    var resultado = await mcp.ChamarFerramentaAsync(ferramenta, JsonDocument.Parse(argumentos).RootElement, CancellationToken.None);
                    resultado.Erro.Should().BeFalse(resultado.Texto);
                }
            }

            return await File.ReadAllBytesAsync(caminho);
        }
        finally
        {
            pasta.Delete(recursive: true);
        }
    }

    private static string Caminho(string secao)
    {
        var caminho = new ConfigurationBuilder()
            .AddUserSecrets("castwork-webapi")
            .AddEnvironmentVariables()
            .Build()[$"{secao}:Caminho"];
        if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
        {
            Assert.Ignore($"{secao} não configurado ({secao}:Caminho): teste com o programa ignorado.");
        }

        return caminho!;
    }
}
