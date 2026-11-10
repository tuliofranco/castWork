using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.UnitTests.Application.Ferramentas.Modelo3d;

/// <summary>
/// Os corpos do modelo 3D com o nome da peça: o único leva o nome; vários, "Nome", "Nome (2)"...; na montagem, cada unidade
/// é "Nome (n)".
/// </summary>
public class NomesDosCorposTests
{
    [Test]
    public void DeveDarOsNomesDaPecaAosCorpos()
    {
        NomesDosCorpos.Com("Elo Infinito 2,5 cm", 1).Should().Equal("Elo Infinito 2,5 cm");
        NomesDosCorpos.Com("Elo", 3).Should().Equal("Elo", "Elo (2)", "Elo (3)");
    }

    [Test]
    public void DeveRenomearSoOsCorposComOutroNome()
        => NomesDosCorpos.Renomeacoes(["Elo", "Corpo 2"], "Elo").Should().Equal(("Corpo 2", "Elo (2)"));

    [Test]
    public void DeveNaoRenomearQuandoOsNomesJaSaoOsDaPeca()
        => NomesDosCorpos.Renomeacoes(["Elo", "Elo (2)"], "Elo").Should().BeEmpty();

    [Test]
    public void DevePassarPorNomesProvisoriosQuandoUmNomeNovoAindaEDeOutroCorpo()
    {
        var renomeacoes = NomesDosCorpos.Renomeacoes(["Elo (2)", "Elo"], "Elo");

        renomeacoes.Should().HaveCount(4);
        renomeacoes.Take(2).Select(r => r.De).Should().Equal("Elo (2)", "Elo");
        renomeacoes.Skip(2).Select(r => r.Para).Should().Equal("Elo", "Elo (2)");
        renomeacoes.Skip(2).Select(r => r.De).Should().Equal(renomeacoes.Take(2).Select(r => r.Para));
    }

    [Test]
    public void DeveCortarONomeLongoParaCaberONumero()
    {
        var nome = NomesDosCorpos.DaUnidade(new string('a', 150), 12);

        nome.Should().HaveLength(NomesDosCorpos.TamanhoMaximo).And.EndWith(" (12)");
    }
}
