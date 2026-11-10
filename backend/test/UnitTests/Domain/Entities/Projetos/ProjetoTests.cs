using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Projetos;

public class ProjetoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CotadoEm = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly PrecosDoAtelie Atelie = PrecosDoAtelieCom(640m, 6.8m, perda: 8m);

    [Test]
    public void DeveCriarProjetoSemVersoes()
    {
        var projeto = Projeto.Criar(" Colar Raízes ", "Inspirado em raízes", clienteId: 3, Atelie, Agora);

        projeto.Should().BeEquivalentTo(new { Nome = "Colar Raízes", Descricao = "Inspirado em raízes", ClienteId = 3L });
        projeto.Versoes.Should().BeEmpty();
    }

    [Test]
    public void DeveNumerarAPrimeiraVersaoComo1EmDesenvolvimento()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);

        var versao = projeto.CriarVersao("Com safira", null);

        versao.Should().BeEquivalentTo(new { Numero = 1, Nome = "Com safira", Status = StatusVersao.EmDesenvolvimento, Itens = Array.Empty<ItemComposicao>() });
    }

    [Test]
    public void DeveAceitarProjetoSemCliente()
    {
        var projeto = Projeto.Criar("Pulseira autoral", null, clienteId: null, Atelie, Agora);

        projeto.ClienteId.Should().BeNull();
    }

    [Test]
    public void DeveNumerarVersoesEmSequencia()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);

        projeto.CriarVersao("Base", null);
        var v2 = projeto.CriarVersao("Com pedra", null);
        var v3 = projeto.CriarVersao("Sem pedra", "mais leve");

        projeto.Versoes.Select(v => (v.Numero, v.Nome)).Should().Equal((1, "Base"), (2, "Com pedra"), (3, "Sem pedra"));
        (v2.Numero, v3.Observacoes).Should().Be((2, "mais leve"));
    }

    [Test]
    public void DeveSomarQuantidadeAoAdicionarAMesmaPecaDeNovo()
    {
        var (projeto, v1) = ProjetoComIds();

        projeto.AdicionarItem(v1, pecaDoProjetoId: 10, quantidade: 8, observacao: null);
        projeto.AdicionarItem(v1, pecaDoProjetoId: 10, quantidade: 4, observacao: "banho de ródio");
        projeto.AdicionarItem(v1, pecaDoProjetoId: 20, quantidade: 1, observacao: null);

        Versao(projeto, v1).Itens.Select(i => (i.PecaDoProjetoId, i.Quantidade, i.Observacao))
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
    public void DevePublicarVersaoSemPecas()
    {
        var (projeto, v1) = ProjetoComIds();

        projeto.PublicarVersao(v1, Agora);

        Versao(projeto, v1).Should().BeEquivalentTo(new { Status = StatusVersao.Publicada, PublicadaEm = (DateTimeOffset?)Agora });
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
        copia.Itens.Select(i => (i.PecaDoProjetoId, i.Quantidade, i.Observacao)).Should().Equal((10L, 12, "ródio"), (20L, 1, (string?)null));
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
            .Which.Should().BeEquivalentTo(new { PecaDoProjetoId = 10L, Quantidade = 14, Observacao = "ajustado ao pulso" });
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
        var projeto = Projeto.Criar("Colar", null, clienteId: null, Atelie, Agora);

        projeto.VincularCliente(7);
        var vinculado = projeto.ClienteId;
        projeto.VincularCliente(null);

        vinculado.Should().Be(7);
        projeto.ClienteId.Should().BeNull();
    }

    [Test]
    public void DeveRenomearOProjetoERecusarNomeVazio()
    {
        var projeto = Projeto.Criar("Colar", null, clienteId: null, Atelie, Agora);

        projeto.Renomear("  Colar Raízes  ");
        var vazio = () => projeto.Renomear(" ");

        projeto.Nome.Should().Be("Colar Raízes");
        vazio.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Nome");
        projeto.Nome.Should().Be("Colar Raízes");
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

    [Test]
    public void DeveNascerComOsPrecosDoAtelieEAData()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);

        projeto.Cotacoes.Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal((Metal.Ouro, 640m, CotadoEm), (Metal.Prata, 6.8m, CotadoEm));
        projeto.ParametrosDePreco.Should().Be(Atelie.Parametros);
        projeto.ParametrosDePreco.Should().NotBeSameAs(Atelie.Parametros, "o projeto tem a sua cópia");
        (projeto.PrecosHerdadosEm, projeto.PrecosAlteradosEm).Should().Be((Agora, (DateTimeOffset?)null));
    }

    [Test]
    public void DeveHerdarSoOsMetaisQueOAtelieJaCotou()
    {
        var semPrata = new PrecosDoAtelie([CotacaoMetal.Registrar(Metal.Ouro, Dinheiro.Criar(640m), CotadoEm, null)], Atelie.Parametros);

        var projeto = Projeto.Criar("Colar", null, null, semPrata, Agora);

        projeto.Cotacoes.Select(c => c.Metal).Should().Equal(Metal.Ouro);
    }

    [Test]
    public void DeveAlterarPrecosSoNoProjetoMantendoADataDaCotacaoQueNaoMudou()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);
        var depois = Agora.AddDays(2);
        var novosParametros = ParametrosDePreco.Criar(10m, Dinheiro.Criar(120m), 2.2m, 5m, Dinheiro.Criar(50m));

        projeto.AlterarPrecos(new Dictionary<Metal, Dinheiro> { [Metal.Ouro] = Dinheiro.Criar(655m), [Metal.Prata] = Dinheiro.Criar(6.8m) }, novosParametros, depois);

        projeto.Cotacoes.Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal((Metal.Ouro, 655m, depois), (Metal.Prata, 6.8m, CotadoEm));
        projeto.ParametrosDePreco.Should().Be(novosParametros);
        (projeto.PrecosHerdadosEm, projeto.PrecosAlteradosEm).Should().Be((Agora, (DateTimeOffset?)depois));
        Atelie.Parametros.PerdaPercentual.Should().Be(8m, "editar o projeto não altera o ateliê");
    }

    [Test]
    public void DeveRemoverACotacaoDoMetalQueNaoFoiInformado()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);

        projeto.AlterarPrecos(new Dictionary<Metal, Dinheiro> { [Metal.Ouro] = Dinheiro.Criar(640m) }, Atelie.Parametros, Agora.AddDays(1));

        projeto.Cotacoes.Select(c => c.Metal).Should().Equal(Metal.Ouro);
    }

    [Test]
    public void DeveRecusarCotacaoZeroNoProjeto()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);

        var alterar = () => projeto.AlterarPrecos(new Dictionary<Metal, Dinheiro> { [Metal.Ouro] = Dinheiro.Zero }, Atelie.Parametros, Agora);

        alterar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Valor");
    }

    [Test]
    public void DeveAtualizarParaOsValoresAtuaisDoAtelieDesfazendoAEdicao()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);
        projeto.AlterarPrecos(new Dictionary<Metal, Dinheiro> { [Metal.Ouro] = Dinheiro.Criar(700m) }, ParametrosDePreco.Criar(12m, Dinheiro.Criar(80m), 3m, 0m, Dinheiro.Zero), Agora.AddDays(1));
        var hoje = Agora.AddDays(10);
        var atelieHoje = PrecosDoAtelieCom(660m, 7.1m, perda: 9m, registradaEm: hoje.AddHours(-1));

        projeto.AtualizarPrecosDoAtelie(atelieHoje, hoje);

        projeto.Cotacoes.Select(c => (c.Metal, c.Valor.Valor, c.CotadaEm)).Should().Equal((Metal.Ouro, 660m, hoje.AddHours(-1)), (Metal.Prata, 7.1m, hoje.AddHours(-1)));
        projeto.ParametrosDePreco.Should().Be(atelieHoje.Parametros);
        (projeto.PrecosHerdadosEm, projeto.PrecosAlteradosEm).Should().Be((hoje, (DateTimeOffset?)null));
    }

    [Test]
    public void DeveHerdarValorDaHoraADefinirSemRecusar()
    {
        // O ateliê começa com a hora "a definir" (zero): o projeto copia como está, sem passar pela validação.
        var aDefinir = new PrecosDoAtelie([], ParametrosComHoraZero());

        var projeto = Projeto.Criar("Colar", null, null, aDefinir, Agora);

        (projeto.ParametrosDePreco.ValorHora, projeto.Cotacoes.Count).Should().Be((Dinheiro.Zero, 0));
    }

    /// <summary>
    /// Fora do banco os Ids são 0; os testes atribuem Ids como o EF faria ao salvar. O projeto tem duas peças importadas
    /// da biblioteca (publicadas), com Ids 10 e 20.
    /// </summary>
    private static (Projeto Projeto, long V1) ProjetoComIds()
    {
        var projeto = Projeto.Criar("Colar", null, null, Atelie, Agora);
        DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(1, "Elo oval")), 10);
        DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(2, "Fecho lagosta")), 20);
        return (projeto, DefinirId(projeto.CriarVersao("Base", null), 100));
    }

    private static Peca PecaDaBiblioteca(long id, string nome)
    {
        var liga = Liga.Criar("Ouro 18k amarelo", Metal.Ouro, TeorMetal.Ouro18k, 15.5m);
        DefinirId(liga, 2);
        var peca = Peca.Criar(nome, TipoPeca.Elo, liga, null, Peso.EmGramas(0.35m), null);
        DefinirId(peca, id);
        return peca;
    }

    private static long DefinirId<T>(T entidade, long id)
        where T : BaseEntity<long>
    {
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(entidade, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return id;
    }

    private static PrecosDoAtelie PrecosDoAtelieCom(decimal ouro, decimal prata, decimal perda, DateTimeOffset? registradaEm = null)
        => new(
            [
                CotacaoMetal.Registrar(Metal.Ouro, Dinheiro.Criar(ouro), registradaEm ?? CotadoEm, null),
                CotacaoMetal.Registrar(Metal.Prata, Dinheiro.Criar(prata), registradaEm ?? CotadoEm, null),
            ],
            ParametrosDePreco.Criar(perda, Dinheiro.Criar(90m), 1.8m, 0m, Dinheiro.Criar(10m)));

    /// <summary>
    /// Como o EF lê a linha do ateliê recém-instalado: valor da hora zero, que a fábrica recusa.
    /// </summary>
    private static ParametrosDePreco ParametrosComHoraZero()
    {
        var parametros = ParametrosDePreco.Criar(8m, Dinheiro.Criar(1m), 1.8m, 0m, Dinheiro.Criar(10m));
        typeof(ParametrosDePreco).GetProperty(nameof(ParametrosDePreco.ValorHora))!.SetValue(parametros, Dinheiro.Zero);
        return parametros;
    }

    private static VersaoProjeto Versao(Projeto projeto, long id) => projeto.Versoes.Single(v => v.Id == id);
}
