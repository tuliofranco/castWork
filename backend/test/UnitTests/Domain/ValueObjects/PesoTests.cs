using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class PesoTests
{
    [Test]
    public void DeveArredondarPesoParaMiligrama()
    {
        var peso = Peso.EmGramas(3.14159m);

        peso.Gramas.Should().Be(3.142m);
    }

    [Test]
    public void DeveRejeitarPesoNegativo()
    {
        var criar = () => Peso.EmGramas(-1);

        criar.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveSomarEMultiplicarPeso()
    {
        var resultado = (Peso.EmGramas(1.5m) + Peso.EmGramas(0.25m)) * 2;

        resultado.Should().Be(Peso.EmGramas(3.5m));
    }
}
