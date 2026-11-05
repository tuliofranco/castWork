using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Projetos;

public class ProjetoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveCriarProjetoComVersao1EmDesenvolvimentoESemItens()
    {
        var projeto = Projeto.Criar(" Colar Raízes ", "Inspirado em raízes", clienteId: 3);

        projeto.Should().BeEquivalentTo(new { Nome = "Colar Raízes", Descricao = "Inspirado em raízes", ClienteId = 3L });
        projeto.Versoes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Numero = 1, Nome = "Versão 1", Status = StatusVersao.EmDesenvolvimento, Itens = Array.Empty<ItemComposicao>() });
    }

    [Test]
    public void DeveAceitarProjetoSemCliente()
    {
        var projeto = Projeto.Criar("Pulseira autoral", null, clienteId: null);

        projeto.ClienteId.Should().BeNull();
    }

    [Test]
    public void DeveNumerarVersoesEmSequencia()
    {
        var projeto = Projeto.Criar("Colar", null, null);

        var v2 = projeto.CriarVersao("Com pedra", null);
        var v3 = projeto.CriarVersao("Sem pedra", "mais leve");

        projeto.Versoes.Select(v => (v.Numero, v.Nome)).Should().Equal((1, "Versão 1"), (2, "Com pedra"), (3, "Sem pedra"));
        (v2.Numero, v3.Observacoes).Should().Be((2, "mais leve"));
    }

    [Test]
    public void DeveSomarQuantidadeAoAdicionarAMesmaPecaDeNovo()
    {
        var (projeto, v1) = ProjetoComIds();

        projeto.AdicionarItem(v1, pecaId: 10, quantidade: 8, observacao: null);
        projeto.AdicionarItem(v1, pecaId: 10, quantidade: 4, observacao: "banho de ródio");
        projeto.AdicionarItem(v1, pecaId: 20, quantidade: 1, observacao: null);

        Versao(projeto, v1).Itens.Select(i => (i.PecaId, i.Quantidade, i.Observacao))
            .Should().Equal((10L, 12, "banho de ródio"), (20L, 1, (string?)null));
    }

    [TestCase(0)]
    [TestCase(10_001)]
    public void DeveRejeitarQuantidadeForaDaFaixa(int quantidade)
    {
        var (projeto, v1) = ProjetoComIds();

        var adicionar = () => projeto.AdicionarItem(v1, 10, quantidade, null);

        adicionar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Quantidade");
    }

    [Test]
    public void DeveExigirAoMenosUmaPecaParaPublicar()
    {
        var (projeto, v1) = ProjetoComIds();

        var publicar = () => projeto.PublicarVersao(v1, Agora);

        publicar.Should().Throw<RegraDeNegocioException>().WithMessage("*ao menos uma peça*");
    }

    [Test]
    public void DeveCongelarComposicaoDaVersaoPublicada()
    {
        var (projeto, v1) = ProjetoComIds();
        projeto.AdicionarItem(v1, 10, 12, null);
        projeto.PublicarVersao(v1, Agora);
        var itemId = DefinirId(Versao(projeto, v1).Itens.Single(), 500);

        var alteracoes = new Action[]
        {
            () => projeto.AdicionarItem(v1, 20, 1, null),
            () => projeto.AlterarItem(v1, itemId, 5, null),
            () => projeto.RemoverItem(v1, itemId),
            () => projeto.PublicarVersao(v1, Agora),
        };

        Versao(projeto, v1).Should().BeEquivalentTo(new { Status = StatusVersao.Publicada, PublicadaEm = (DateTimeOffset?)Agora });
        alteracoes.Should().AllSatisfy(a => a.Should().Throw<RegraDeNegocioException>().WithMessage("*publicada*Duplique*"));
    }

    [Test]
    public void DeveDuplicarVersaoPublicadaCopiandoComposicaoParaNovaVersaoEmDesenvolvimento()
    {
        var (projeto, v1) = ProjetoComIds();
        projeto.AdicionarItem(v1, 10, 12, "ródio");
        projeto.AdicionarItem(v1, 20, 1, null);
        projeto.PublicarVersao(v1, Agora);

        var copia = projeto.DuplicarVersao(v1, nome: null);
        copia.Should().BeEquivalentTo(new { Numero = 2, Nome = "Versão 2", Status = StatusVersao.EmDesenvolvimento });
        copia.Itens.Select(i => (i.PecaId, i.Quantidade, i.Observacao)).Should().Equal((10L, 12, "ródio"), (20L, 1, (string?)null));
        copia.Itens.Should().NotIntersectWith(Versao(projeto, v1).Itens, "a cópia tem itens próprios; mexer nela não altera a publicada");
    }

    [Test]
    public void DeveAlterarERemoverItemDaVersaoEmDesenvolvimento()
    {
        var (projeto, v1) = ProjetoComIds();
        projeto.AdicionarItem(v1, 10, 12, null);
        projeto.AdicionarItem(v1, 20, 1, null);
        var elo = DefinirId(Versao(projeto, v1).Itens.First(), 1);
        var fecho = DefinirId(Versao(projeto, v1).Itens.Last(), 2);

        projeto.AlterarItem(v1, elo, 14, "ajustado ao pulso");
        projeto.RemoverItem(v1, fecho);

        Versao(projeto, v1).Itens.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PecaId = 10L, Quantidade = 14, Observacao = "ajustado ao pulso" });
    }

    [Test]
    public void DeveArquivarVersaoEmDesenvolvimentoEBloquearAlteracoesAteDesarquivar()
    {
        var (projeto, v1) = ProjetoComIds();

        projeto.ArquivarVersao(v1);
        var alteracoes = new Action[]
        {
            () => projeto.AdicionarItem(v1, 10, 1, null),
            () => projeto.PublicarVersao(v1, Agora),
            () => projeto.ArquivarVersao(v1),
        };

        Versao(projeto, v1).Status.Should().Be(StatusVersao.Arquivada);
        alteracoes.Should().AllSatisfy(a => a.Should().Throw<RegraDeNegocioException>().WithMessage("*arquivada*Desarquive*"));

        projeto.DesarquivarVersao(v1);
        projeto.AdicionarItem(v1, 10, 1, null);
        Versao(projeto, v1).Should().BeEquivalentTo(new { Status = StatusVersao.EmDesenvolvimento }, o => o.ExcludingMissingMembers());
    }

    [Test]
    public void DeveImpedirArquivarVersaoPublicada()
    {
        var (projeto, v1) = ProjetoComIds();
        projeto.AdicionarItem(v1, 10, 1, null);
        projeto.PublicarVersao(v1, Agora);

        var arquivar = () => projeto.ArquivarVersao(v1);

        arquivar.Should().Throw<RegraDeNegocioException>().WithMessage("*publicada*");
        Versao(projeto, v1).Status.Should().Be(StatusVersao.Publicada);
    }

    [Test]
    public void DeveRecusarDesarquivarVersaoQueNaoEstaArquivada()
    {
        var (projeto, v1) = ProjetoComIds();

        var desarquivar = () => projeto.DesarquivarVersao(v1);

        desarquivar.Should().Throw<RegraDeNegocioException>().WithMessage("*não está arquivada*");
    }

    [Test]
    public void DeveVincularEDesvincularCliente()
    {
        var projeto = Projeto.Criar("Colar", null, clienteId: null);

        projeto.VincularCliente(7);
        var vinculado = projeto.ClienteId;
        projeto.VincularCliente(null);

        vinculado.Should().Be(7);
        projeto.ClienteId.Should().BeNull();
    }

    [Test]
    public void DeveLancarNaoEncontradoParaVersaoOuItemInexistente()
    {
        var (projeto, v1) = ProjetoComIds();

        var versaoInexistente = () => projeto.AdicionarItem(999, 10, 1, null);
        var itemInexistente = () => projeto.RemoverItem(v1, 999);

        versaoInexistente.Should().Throw<EntidadeNaoEncontradaException>().Which.Entidade.Should().Be("Versão");
        itemInexistente.Should().Throw<EntidadeNaoEncontradaException>().Which.Entidade.Should().Be("Item da composição");
    }

    /// <summary>
    /// Fora do banco os Ids são 0; os testes atribuem Ids como o EF faria ao salvar.
    /// </summary>
    private static (Projeto Projeto, long V1) ProjetoComIds()
    {
        var projeto = Projeto.Criar("Colar", null, null);
        return (projeto, DefinirId(projeto.Versoes.Single(), 100));
    }

    private static long DefinirId<T>(T entidade, long id)
        where T : BaseEntity<long>
    {
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(entidade, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return id;
    }

    private static VersaoProjeto Versao(Projeto projeto, long id) => projeto.Versoes.Single(v => v.Id == id);
}
