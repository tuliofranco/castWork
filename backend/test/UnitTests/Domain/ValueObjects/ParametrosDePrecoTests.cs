using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class ParametrosDePrecoTests
{
    [Test]
    public void DeveCriarParametrosValidos()
    {
        var parametros = Criar(perda: 8m, valorHora: 85.5m, markup: 1.8m, taxas: 6.5m, arredondarPara: 10m);

        parametros.Should().BeEquivalentTo(new
        {
            PerdaPercentual = 8m,
            ValorHora = Dinheiro.Criar(85.5m),
            Markup = 1.8m,
            TaxasPercentual = 6.5m,
            ArredondarPara = Dinheiro.Criar(10m),
        });
    }

    [TestCase(-0.01, 0, "PerdaPercentual")]
    [TestCase(50.01, 0, "PerdaPercentual")]
    [TestCase(0, -0.01, "TaxasPercentual")]
    [TestCase(0, 50.01, "TaxasPercentual")]
    public void DeveRecusarPercentuaisForaDaFaixa(decimal perda, decimal taxas, string campo)
    {
        var criar = () => Criar(perda: perda, taxas: taxas);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(campo);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(10.01)]
    public void DeveRecusarMarkupZeroNegativoOuExagerado(decimal markup)
    {
        var criar = () => Criar(markup: markup);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(ParametrosDePreco.Markup));
    }

    [Test]
    public void DeveRecusarValorDaHoraZero()
    {
        var criar = () => Criar(valorHora: 0m);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(ParametrosDePreco.ValorHora));
    }

    [Test]
    public void DeveAceitarPerdaETaxasZeroESemArredondamento()
    {
        var parametros = Criar(perda: 0m, taxas: 0m, arredondarPara: 0m);

        parametros.Arredondar(Dinheiro.Criar(1234.56m)).Should().Be(Dinheiro.Criar(1234.56m));
    }

    [TestCase(1231.01, 1240)]
    [TestCase(1240, 1240)]
    [TestCase(0.01, 10)]
    public void DeveArredondarParaCimaAoMultiplo(decimal preco, decimal esperado)
    {
        var parametros = Criar(arredondarPara: 10m);

        parametros.Arredondar(Dinheiro.Criar(preco)).Should().Be(Dinheiro.Criar(esperado));
    }

    private static ParametrosDePreco Criar(decimal perda = 8m, decimal valorHora = 50m, decimal markup = 1.8m, decimal taxas = 0m, decimal arredondarPara = 10m)
        => ParametrosDePreco.Criar(perda, Dinheiro.Criar(valorHora), markup, taxas, Dinheiro.Criar(arredondarPara));
}
