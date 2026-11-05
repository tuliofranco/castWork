using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class DinheiroTests
{
    [TestCase(10.005, 10.01)]
    [TestCase(10.004, 10.00)]
    [TestCase(0.125, 0.13)]
    public void DeveArredondarDinheiroParaDuasCasasMeioParaCima(decimal valor, decimal esperado)
    {
        var dinheiro = Dinheiro.Criar(valor);

        dinheiro.Valor.Should().Be(esperado);
    }

    [Test]
    public void DeveRejeitarDinheiroNegativo()
    {
        var criar = () => Dinheiro.Criar(-0.01m);

        criar.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveOperarDinheiroMantendoArredondamento()
    {
        var a = Dinheiro.Criar(100m);
        var b = Dinheiro.Criar(33.33m);

        var resultado = new { Soma = a + b, Diferenca = a - b, Produto = b * 3, ComMargem = b.AcrescentarPercentual(30) };

        resultado.Should().BeEquivalentTo(new
        {
            Soma = Dinheiro.Criar(133.33m),
            Diferenca = Dinheiro.Criar(66.67m),
            Produto = Dinheiro.Criar(99.99m),
            ComMargem = Dinheiro.Criar(43.33m),
        });
    }

    [Test]
    public void DeveCompararDinheiroPorValor()
    {
        var resultado = Dinheiro.Criar(1.5m) == Dinheiro.Criar(1.50m) && Dinheiro.Criar(2) > Dinheiro.Criar(1);

        resultado.Should().BeTrue();
    }

    [Test]
    public void DeveImpedirSubtracaoQueResulteEmNegativo()
    {
        var subtrair = () => Dinheiro.Criar(1) - Dinheiro.Criar(2);

        subtrair.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveFormatarDinheiroEmReais()
    {
        var texto = Dinheiro.Criar(1234.5m).ToString();

        // O espaço entre "R$" e o valor varia conforme o ICU do SO (U+00A0 ou U+202F).
        texto.Should().MatchRegex(@"^R\$\s1\.234,50$");
    }
}
