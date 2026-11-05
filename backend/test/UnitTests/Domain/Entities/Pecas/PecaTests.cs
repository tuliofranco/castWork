using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Pecas;

public class PecaTests
{
    [Test]
    public void DeveCriarPecaAtivaComDadosNormalizados()
    {
        var peca = Peca.Criar("  Elo oval ", TipoPeca.Elo, " Ouro amarelo ", TeorMetal.Ouro18k, Peso.EmGramas(0.35m), "  ");

        peca.Should().BeEquivalentTo(new
        {
            Nome = "Elo oval",
            Tipo = TipoPeca.Elo,
            Material = "Ouro amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.35m),
            Descricao = (string?)null,
            Ativo = true,
        });
    }

    [Test]
    public void DeveRejeitarTipoForaDaLista()
    {
        var criar = () => Peca.Criar("Elo", (TipoPeca)42, null, null, null, null);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Tipo");
    }

    [Test]
    public void DeveDesativarEReativarPeca()
    {
        var peca = Peca.Criar("Fecho lagosta", TipoPeca.Fecho, null, null, null, null);

        peca.Desativar();
        var aposDesativar = peca.Ativo;
        peca.Reativar();

        aposDesativar.Should().BeFalse();
        peca.Ativo.Should().BeTrue();
    }
}
