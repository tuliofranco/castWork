using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Precificacao;

public class CotacaoMetalTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveRegistrarCotacaoDoMetalPuroComDataEAutor()
    {
        var cotacao = CotacaoMetal.Registrar(Metal.Ouro, Dinheiro.Criar(612.35m), Agora, 7);

        cotacao.Should().BeEquivalentTo(new { Metal = Metal.Ouro, Valor = Dinheiro.Criar(612.35m), RegistradaEm = Agora, RegistradaPor = 7L });
    }

    [Test]
    public void DeveRecusarCotacaoZero()
    {
        var registrar = () => CotacaoMetal.Registrar(Metal.Prata, Dinheiro.Zero, Agora, null);

        registrar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(CotacaoMetal.Valor));
    }

    [Test]
    public void DeveRecusarMetalForaDaLista()
    {
        var registrar = () => CotacaoMetal.Registrar((Metal)42, Dinheiro.Criar(10m), Agora, null);

        registrar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(CotacaoMetal.Metal));
    }
}
