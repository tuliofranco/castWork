using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class TeorMetalTests
{
    [TestCase("18k", 750, 18.0)]
    [TestCase("18K", 750, 18.0)]
    [TestCase(" 14k ", 585, 14.0)]
    [TestCase("10k", 417, 10.0)]
    [TestCase("22k", 917, 22.0)]
    [TestCase("925", 925, 22.2)]
    [TestCase("750", 750, 18.0)]
    public void DeveInterpretarTeorEmQuilatesOuMilesimos(string texto, int milesimos, decimal quilates)
    {
        var teor = TeorMetal.Parse(texto);

        teor.Should().BeEquivalentTo(new { Milesimos = milesimos, Quilates = quilates });
    }

    [TestCase("0")]
    [TestCase("1001")]
    [TestCase("25k")]
    [TestCase("ouro")]
    public void DeveRejeitarTeorInvalido(string texto)
    {
        var parse = () => TeorMetal.Parse(texto);

        parse.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveCalcularPurezaDoTeor()
    {
        var pureza = TeorMetal.Ouro18k.Pureza;

        pureza.Should().Be(0.75m);
    }
}
