using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Castwork.UnitTests.Infra.IA;

/// <summary>
/// A medição do modelo 3D pelo SolidCraft: a leitura do <c>measure</c> e do <c>inspect.interference</c> (como o
/// SolidCraft 0.40 devolve) e a medição guardada pelo arquivo e pelos materiais.
/// </summary>
public class MedicaoNoArquivoTests
{
    private const string Medida = """
        {
          "bodies": [
            { "name": "Aro", "closed": true, "material": "Castwork liga 2", "density_g_cm3": 15.5, "volume_mm3": 157.07963267839978, "mass_g": 2.4347343065151965 },
            { "name": "Pino", "closed": true, "material": "Default", "density_g_cm3": 1.29, "volume_mm3": 10.0, "mass_g": 0.0129 },
            { "name": "Aba", "closed": false, "material": "Castwork liga 2", "area_mm2": 12.5 }
          ],
          "body_count": 3,
          "total": { "open_bodies_skipped": 1 }
        }
        """;

    private const string Interferencia = """
        {
          "count": 1,
          "interferences": [
            { "a": "Aro", "b": "Garra", "area_mm2": 42.3, "volume_mm3": 19.39831273200989, "center": [3.6, 0, 1] }
          ],
          "unchecked": []
        }
        """;

    [Test]
    public void DeveLerOsCorposDoMeasureComMaterialVolumeMassaEFechado()
    {
        var corpos = MedicaoNoArquivo.LerCorpos(Medida);

        corpos.Should().HaveCount(3);
        corpos![0].Should().BeEquivalentTo(new { Nome = "Aro", Material = "Castwork liga 2", Fechado = true });
        corpos[0].MassaGramas.Should().BeApproximately(2.4347343m, 0.0000001m);
        corpos[0].VolumeMm3.Should().BeApproximately(157.0796327m, 0.0000001m);
        corpos[1].Material.Should().Be("Default");
        corpos[2].Should().BeEquivalentTo(new CorpoMedido("Aba", "Castwork liga 2", null, null, Fechado: false), "corpo aberto não tem volume nem massa");
    }

    [Test]
    public void DeveLerAsSobreposicoesDoInspectInterference()
        => MedicaoNoArquivo.LerSobreposicoes(Interferencia).Should().Equal(new SobreposicaoMedida("Aro", "Garra", 19.398m));

    [TestCase("{}")]
    [TestCase("não é json")]
    public void DeveRecusarUmResultadoInesperado(string texto)
    {
        MedicaoNoArquivo.LerCorpos(texto).Should().BeNull();
        MedicaoNoArquivo.LerSobreposicoes(texto).Should().BeNull();
    }

    [Test]
    public void DevePedirASobreposicaoComTempoLimitado()
        => MedicaoNoArquivo.Interferencia.Should().Be(("execute", """{"command":"inspect.interference","params":{"budget_s":20}}"""));

    [Test]
    public async Task DeveDevolverAMedicaoGuardadaSemLerOArquivoDeNovoAsync()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var materiais = Materiais(15.5m);
        var guardada = new MedicaoDoModelo([new CorpoMedido("Aro", "Castwork liga 2", 100m, 1.55m, Fechado: true)], []);
        cache.Set(MedidorDoModeloSolidCraft.Chave(7, MateriaisDoModelo.ParaPreparo(materiais)), guardada);
        var medidor = Medidor(cache);
        var leituras = 0;

        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(7, _ => Task.FromResult(new byte[leituras++])), materiais, CancellationToken.None);

        medicao.Should().BeSameAs(guardada);
        leituras.Should().Be(0, "o arquivo não mudou: não lê nem mede de novo");
    }

    [Test]
    public void DeveMudarAChaveQuandoOArquivoOuADensidadeMudam()
    {
        var chave = MedidorDoModeloSolidCraft.Chave(7, MateriaisDoModelo.ParaPreparo(Materiais(15.5m)));

        MedidorDoModeloSolidCraft.Chave(8, MateriaisDoModelo.ParaPreparo(Materiais(15.5m))).Should().NotBe(chave, "salvar o arquivo cria um id novo");
        MedidorDoModeloSolidCraft.Chave(7, MateriaisDoModelo.ParaPreparo(Materiais(15.6m))).Should().NotBe(chave, "a densidade muda a massa");
        MedidorDoModeloSolidCraft.Chave(7, MateriaisDoModelo.ParaPreparo(Materiais(15.5m))).Should().Be(chave);
    }

    [Test]
    public async Task DeveDevolverNuloSemSolidCraftENaoGuardarAFalhaAsync()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var medidor = Medidor(cache);

        var medicao = await medidor.MedirAsync(new ArquivoParaMedir(7, _ => Task.FromResult(new byte[1])), Materiais(15.5m), CancellationToken.None);

        medicao.Should().BeNull();
        cache.Count.Should().Be(0);
    }

    private static MedidorDoModeloSolidCraft Medidor(IMemoryCache cache)
        => new(Options.Create(new SolidCraftOptions { Caminho = "/nao/existe/solidcraft-cli" }), cache, NullLogger<MedidorDoModeloSolidCraft>.Instance);

    private static IReadOnlyList<MaterialDoModelo> Materiais(decimal densidadeDoOuro)
        => MateriaisDoModelo.Montar([(2, "Ouro 18k amarelo", Castwork.Domain.Entities.Precificacao.Metal.Ouro, densidadeDoOuro)]);
}
