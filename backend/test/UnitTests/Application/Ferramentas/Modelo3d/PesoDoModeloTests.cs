using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.DomainService.Precificacao;

namespace Castwork.UnitTests.Application.Ferramentas.Modelo3d;

/// <summary>
/// A classificação de cada corpo medido pelo material do arquivo: liga do ateliê pelo nome estável, "Pedra", "Apoio (não
/// conta)" e sem material (o "Default" do SolidCraft, nenhum ou um material que não é do ateliê).
/// </summary>
public class PesoDoModeloTests
{
    [TestCase("Castwork liga 2", UsoDoCorpo.Metal)]
    [TestCase("Pedra", UsoDoCorpo.Pedra)]
    [TestCase("Apoio (não conta)", UsoDoCorpo.Apoio)]
    [TestCase("Default", UsoDoCorpo.SemMaterial)]
    [TestCase("Steel", UsoDoCorpo.SemMaterial)]
    [TestCase("Castwork liga x", UsoDoCorpo.SemMaterial)]
    [TestCase(null, UsoDoCorpo.SemMaterial)]
    public void DeveClassificarOCorpoPeloMaterial(string? material, UsoDoCorpo uso)
        => PesoDoModelo.Uso(material).Should().Be(uso);

    [Test]
    public void DeveMontarOModeloMedidoComALigaEAMassaDeCadaCorpo()
    {
        var medicao = new MedicaoDoModelo(
            [
                new CorpoMedido("Aro", "Castwork liga 2", 157.0796m, 2.4347343m, Fechado: true),
                new CorpoMedido("Brilhante", "Pedra", 25.13m, 0.088m, Fechado: true),
                new CorpoMedido("Aba", "Castwork liga 3", null, null, Fechado: false),
            ],
            [new SobreposicaoMedida("Aro", "Aba", 1.5m)]);

        var modelo = PesoDoModelo.Classificar(medicao);

        modelo.Medido.Should().BeTrue();
        modelo.Corpos.Should().Equal(
            new CorpoDoModelo("Aro", UsoDoCorpo.Metal, 2, 2.4347343m, Fechado: true),
            new CorpoDoModelo("Brilhante", UsoDoCorpo.Pedra, null, 0.088m, Fechado: true),
            new CorpoDoModelo("Aba", UsoDoCorpo.Metal, 3, null, Fechado: false));
        modelo.Sobreposicoes.Should().Equal(new SobreposicaoDeCorpos("Aro", "Aba", 1.5m));
    }

    [Test]
    public void DeveMedirOPesoDaPecaTodoNaLigaDelaSemPedraApoioNemCorpoAberto()
    {
        var medicao = new MedicaoDoModelo(
            [
                new CorpoMedido("Aro", "Castwork liga 3", 100m, 1m, Fechado: true),
                new CorpoMedido("Garra", "Default", 20m, 0.026m, Fechado: true),
                new CorpoMedido("Brilhante", "Pedra", 25m, 0.0875m, Fechado: true),
                new CorpoMedido("Guia", "Apoio (não conta)", 300m, 0.3m, Fechado: true),
                new CorpoMedido("Aba", "Castwork liga 2", null, null, Fechado: false),
            ],
            []);

        var peso = PesoDoModelo.PesoNaLiga(medicao, 15.5m);

        peso.Gramas.Should().Be(1.86m, "aro + garra = 120 mm³ × 15,5 g/cm³, mesmo com o aro em outra liga e a garra sem material");
    }
}
