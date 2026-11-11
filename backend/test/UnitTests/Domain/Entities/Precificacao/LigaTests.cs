using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Precificacao;

public class LigaTests
{
    [Test]
    public void DeveCriarLigaAtivaComTeorEDensidade()
    {
        var liga = Liga.Criar(" Ouro 18k amarelo ", Metal.Ouro, TeorMetal.Ouro18k, 15.5m);

        liga.Should().BeEquivalentTo(new { Nome = "Ouro 18k amarelo", Metal = Metal.Ouro, Teor = TeorMetal.Ouro18k, Densidade = 15.5m, Ativo = true });
        liga.Teor.Pureza.Should().Be(0.75m);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(25.001)]
    public void DeveRecusarDensidadeForaDaFaixa(decimal densidade)
    {
        var criar = () => Liga.Criar("Prata 925", Metal.Prata, TeorMetal.Prata925, densidade);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(Liga.Densidade));
    }

    [Test]
    public void DeveArredondarDensidadeEmTresCasas()
    {
        var liga = Liga.Criar("Prata 925", Metal.Prata, TeorMetal.Prata925, 10.36049m);

        liga.Densidade.Should().Be(10.360m);
    }

    [Test]
    public void DeveRecusarNomeVazioEMetalForaDaLista()
    {
        var semNome = () => Liga.Criar(" ", Metal.Ouro, TeorMetal.Ouro18k, 15.5m);
        var semMetal = () => Liga.Criar("Platina", (Metal)42, TeorMetal.EmMilesimos(950), 20m);

        semNome.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(Liga.Nome));
        semMetal.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(Liga.Metal));
    }

    [Test]
    public void DeveAlterarEDesativarLiga()
    {
        var liga = Liga.Criar("Ouro 18k", Metal.Ouro, TeorMetal.Ouro18k, 15.5m);

        liga.Alterar("Ouro 18k rosé", Metal.Ouro, TeorMetal.Ouro18k, 15.1m);
        liga.Desativar();

        liga.Should().BeEquivalentTo(new { Nome = "Ouro 18k rosé", Densidade = 15.1m, Ativo = false });
    }
}
