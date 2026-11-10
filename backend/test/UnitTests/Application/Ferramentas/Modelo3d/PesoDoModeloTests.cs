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
}
