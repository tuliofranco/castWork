using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Pecas;

public class PecaTests
{
    [Test]
    public void DeveCriarPecaAtivaComDadosNormalizados()
    {
        var peca = Peca.Criar("  Elo oval ", TipoPeca.Elo, " Ouro amarelo ", TeorMetal.Ouro18k, Peso.EmGramas(0.35m), "  ", null);

        peca.Should().BeEquivalentTo(new
        {
            Nome = "Elo oval",
            Tipo = TipoPeca.Elo,
            Material = "Ouro amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.35m),
            Descricao = (string?)null,
            Custo = (Dinheiro?)null,
            Ativo = true,
        });
    }

    [Test]
    public void DeveCriarPecaComCustoPorUnidade()
    {
        var peca = Peca.Criar("Turmalina 4 mm", TipoPeca.Pedra, "Turmalina", null, null, null, Dinheiro.Criar(85.505m));

        peca.Custo.Should().Be(Dinheiro.Criar(85.51m), "o custo é dinheiro: duas casas, arredondamento comercial");
    }

    [Test]
    public void DeveDefinirETirarOCustoSemMexerNosOutrosDados()
    {
        var peca = Peca.Criar("Fecho lagosta", TipoPeca.Fecho, "Ouro amarelo", TeorMetal.Ouro18k, Peso.EmGramas(0.4m), null, null);

        peca.DefinirCusto(Dinheiro.Criar(120m));
        var comCusto = peca.Custo;
        peca.Atualizar("Fecho lagosta grande", TipoPeca.Fecho, "Ouro amarelo", TeorMetal.Ouro18k, Peso.EmGramas(0.5m), null);
        var aposAtualizar = peca.Custo;
        peca.DefinirCusto(null);

        comCusto.Should().Be(Dinheiro.Criar(120m));
        aposAtualizar.Should().Be(Dinheiro.Criar(120m), "alterar os dados da peça não mexe no custo");
        peca.Custo.Should().BeNull();
    }

    [Test]
    public void DeveRejeitarTipoForaDaLista()
    {
        var criar = () => Peca.Criar("Elo", (TipoPeca)42, null, null, null, null, null);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Tipo");
    }

    [Test]
    public void DeveDesativarEReativarPeca()
    {
        var peca = Peca.Criar("Fecho lagosta", TipoPeca.Fecho, null, null, null, null, null);

        peca.Desativar();
        var aposDesativar = peca.Ativo;
        peca.Reativar();

        aposDesativar.Should().BeFalse();
        peca.Ativo.Should().BeTrue();
    }
}
