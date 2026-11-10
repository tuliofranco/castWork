using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Projetos;

/// <summary>
/// Ciclo de vida da peça do projeto: criar (rascunho), importar (publicada), valor, alterar dados (vira outra peça, também
/// ao trocar a liga) e publicar, e a publicação da versão com peça em rascunho. A liga vai e volta da biblioteca com o Id e
/// a cópia do nome e do teor.
/// </summary>
public class PecaDoProjetoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 13, 0, 0, TimeSpan.Zero);

    private static readonly TeorMetal Ouro18k = TeorMetal.Ouro18k;

    [Test]
    public void DeveCriarPecaNoProjetoEmRascunhoSemVinculoESemValor()
    {
        var projeto = NovoProjeto();

        var peca = projeto.CriarPeca(" Elo oval ", TipoPeca.Elo, NovaLiga(2, "Ouro 18k amarelo"), null, Peso.EmGramas(0.35m), "  ");

        peca.Should().BeEquivalentTo(new
        {
            Nome = "Elo oval",
            Tipo = TipoPeca.Elo,
            LigaId = (long?)2,
            Material = "Ouro 18k amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.35m),
            Descricao = (string?)null,
            Ativo = true,
            Valor = (Dinheiro?)null,
            Status = StatusPecaDoProjeto.Rascunho,
            PecaId = (long?)null,
        });
        projeto.Pecas.Should().ContainSingle().Which.Should().BeSameAs(peca);
    }

    [Test]
    public void DeveCriarPedraComMaterialLivreERecusarLigaDesativadaNaPecaNova()
    {
        var projeto = NovoProjeto();
        var desativada = NovaLiga(5, "Ouro 14k amarelo");
        desativada.Desativar();

        var pedra = projeto.CriarPeca("Turmalina 4 mm", TipoPeca.Pedra, null, "Turmalina", null, null);
        var comLigaDesativada = () => projeto.CriarPeca("Elo", TipoPeca.Elo, desativada, null, null, null);

        pedra.Should().BeEquivalentTo(new { LigaId = (long?)null, Material = "Turmalina", Teor = (TeorMetal?)null });
        comLigaDesativada.Should().Throw<RegraDeNegocioException>().WithMessage("*Ouro 14k amarelo está desativada*");
        projeto.Pecas.Should().ContainSingle();
    }

    [Test]
    public void DeveImportarCopiaPublicadaVinculadaESemValor()
    {
        var projeto = NovoProjeto();
        var biblioteca = PecaDaBiblioteca(7, "Turmalina 4 mm", TipoPeca.Pedra);

        var copia = projeto.ImportarPeca(biblioteca);

        copia.Should().BeEquivalentTo(new
        {
            Nome = "Turmalina 4 mm",
            Tipo = TipoPeca.Pedra,
            biblioteca.Material,
            biblioteca.Peso,
            Valor = (Dinheiro?)null,
            Status = StatusPecaDoProjeto.Publicada,
            PecaId = (long?)7,
        });
    }

    [Test]
    public void DeveImportarEPublicarLevandoALigaEACopiaMesmoComALigaJaEditadaOuDesativada()
    {
        var projeto = NovoProjeto();
        var liga = NovaLiga(2, "Ouro 18k amarelo");
        var biblioteca = ComId(Peca.Criar("Elo oval", TipoPeca.Elo, liga, null, null, null), 7);
        var rascunho = DefinirId(projeto.CriarPeca("Elo torcido", TipoPeca.Elo, liga, null, null, null), 70);
        liga.Alterar("Ouro amarelo 18k", Metal.Ouro, TeorMetal.EmMilesimos(760), 15.6m);
        liga.Desativar();

        var importada = projeto.ImportarPeca(biblioteca);
        var publicada = projeto.PublicarPeca(rascunho);

        importada.Should().BeEquivalentTo(new { LigaId = (long?)2, Material = "Ouro 18k amarelo", Teor = Ouro18k });
        publicada.Should().BeEquivalentTo(new { LigaId = (long?)2, Material = "Ouro 18k amarelo", Teor = Ouro18k });
    }

    [Test]
    public void DeveRecusarImportarAMesmaPecaDuasVezesOuPecaDesativada()
    {
        var projeto = NovoProjeto();
        var turmalina = PecaDaBiblioteca(7, "Turmalina", TipoPeca.Pedra);
        var desativada = PecaDaBiblioteca(8, "Fecho antigo", TipoPeca.Fecho);
        desativada.Desativar();
        projeto.ImportarPeca(turmalina);

        var deNovo = () => projeto.ImportarPeca(turmalina);
        var inativa = () => projeto.ImportarPeca(desativada);

        deNovo.Should().Throw<RegraDeNegocioException>().WithMessage("*Turmalina já está neste projeto*");
        inativa.Should().Throw<RegraDeNegocioException>().WithMessage("*desativada*");
        projeto.Pecas.Should().ContainSingle();
    }

    [Test]
    public void DeveDefinirValorSemMudarStatusNemVinculo()
    {
        var projeto = NovoProjeto();
        var id = DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(7, "Turmalina", TipoPeca.Pedra)), 70);

        projeto.DefinirValorDaPeca(id, Dinheiro.Criar(85.505m));
        var comValor = Obter(projeto, id).Valor;
        projeto.DefinirValorDaPeca(id, null);

        comValor.Should().Be(Dinheiro.Criar(85.51m), "o valor é dinheiro: duas casas, arredondamento comercial");
        Obter(projeto, id).Should().BeEquivalentTo(new { Valor = (Dinheiro?)null, Status = StatusPecaDoProjeto.Publicada, PecaId = (long?)7 });
    }

    [Test]
    public void DeveVirarOutraPecaAoAlterarOsDadosDeUmaPublicada()
    {
        var projeto = NovoProjeto();
        var id = DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(7, "Turmalina", TipoPeca.Pedra)), 70);
        projeto.DefinirValorDaPeca(id, Dinheiro.Criar(85m));

        var virouOutra = projeto.AlterarPeca(id, "Turmalina 5 mm", TipoPeca.Pedra, null, "Turmalina", null, null, ativo: true);

        virouOutra.Should().BeTrue();
        Obter(projeto, id).Should().BeEquivalentTo(new
        {
            Nome = "Turmalina 5 mm",
            Status = StatusPecaDoProjeto.Rascunho,
            PecaId = (long?)null,
            Valor = (Dinheiro?)Dinheiro.Criar(85m),
        });
    }

    [Test]
    public void DeveContinuarPublicadaAoAlterarSoOAtivoOuRepetirOsMesmosDadosComAMesmaLigaJaDesativada()
    {
        var projeto = NovoProjeto();
        var liga = NovaLiga(2, "Ouro 18k amarelo");
        var id = DefinirId(projeto.ImportarPeca(ComId(Peca.Criar("Elo oval", TipoPeca.Elo, liga, null, Peso.EmGramas(0.35m), null), 7)), 70);
        liga.Alterar("Ouro amarelo 18k", Metal.Ouro, Ouro18k, 15.5m);
        liga.Desativar();

        var desativar = projeto.AlterarPeca(id, " Elo oval ", TipoPeca.Elo, liga, null, Peso.EmGramas(0.350m), null, ativo: false);

        desativar.Should().BeFalse("espaços e zeros à direita não mudam o dado, a mesma liga mantém a cópia e ativo não é identidade");
        Obter(projeto, id).Should().BeEquivalentTo(new
        {
            Ativo = false,
            Status = StatusPecaDoProjeto.Publicada,
            PecaId = (long?)7,
            LigaId = (long?)2,
            Material = "Ouro 18k amarelo",
        });
    }

    [Test]
    public void DeveVirarOutraPecaAoTrocarALigaDeUmaPublicadaERecusarTrocarParaUmaDesativada()
    {
        var projeto = NovoProjeto();
        var amarelo = NovaLiga(2, "Ouro 18k amarelo");
        var id = DefinirId(projeto.ImportarPeca(ComId(Peca.Criar("Elo oval", TipoPeca.Elo, amarelo, null, null, null), 7)), 70);
        var desativada = NovaLiga(5, "Ouro 14k amarelo");
        desativada.Desativar();

        var paraDesativada = () => projeto.AlterarPeca(id, "Elo oval", TipoPeca.Elo, desativada, null, null, null, ativo: true);
        var recusada = Obter(projeto, id).Status;
        var virouOutra = projeto.AlterarPeca(id, "Elo oval", TipoPeca.Elo, NovaLiga(3, "Ouro 18k branco"), null, null, null, ativo: true);

        paraDesativada.Should().Throw<RegraDeNegocioException>().WithMessage("*Ouro 14k amarelo está desativada*");
        recusada.Should().Be(StatusPecaDoProjeto.Publicada);
        virouOutra.Should().BeTrue("trocar a liga é alterar os dados");
        Obter(projeto, id).Should().BeEquivalentTo(new
        {
            Status = StatusPecaDoProjeto.Rascunho,
            PecaId = (long?)null,
            LigaId = (long?)3,
            Material = "Ouro 18k branco",
            Teor = Ouro18k,
        });
    }

    [Test]
    public void DevePublicarRascunhoCriandoPecaNaBibliotecaSemValor()
    {
        var projeto = NovoProjeto();
        var id = DefinirId(projeto.CriarPeca("Elo torcido", TipoPeca.Elo, NovaLiga(3, "Ouro 18k branco"), null, Peso.EmGramas(0.5m), "Feito à mão"), 70);
        projeto.DefinirValorDaPeca(id, Dinheiro.Criar(40m));

        var publicada = projeto.PublicarPeca(id);

        publicada.Should().BeEquivalentTo(new
        {
            Nome = "Elo torcido",
            Tipo = TipoPeca.Elo,
            LigaId = (long?)3,
            Material = "Ouro 18k branco",
            Teor = Ouro18k,
            Peso = Peso.EmGramas(0.5m),
            Descricao = "Feito à mão",
            Ativo = true,
        });
        Obter(projeto, id).Should().BeEquivalentTo(new { Status = StatusPecaDoProjeto.Publicada, Valor = (Dinheiro?)Dinheiro.Criar(40m) });
    }

    [Test]
    public void DeveRecusarPublicarPecaJaPublicadaOuDesativada()
    {
        var projeto = NovoProjeto();
        var importada = DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(7, "Elo oval", TipoPeca.Elo)), 70);
        var desativada = DefinirId(projeto.CriarPeca("Fecho", TipoPeca.Fecho, null, null, null, null), 71);
        projeto.AlterarPeca(desativada, "Fecho", TipoPeca.Fecho, null, null, null, null, ativo: false);

        var publicarImportada = () => projeto.PublicarPeca(importada);
        var publicarDesativada = () => projeto.PublicarPeca(desativada);
        var inexistente = () => projeto.PublicarPeca(999);

        publicarImportada.Should().Throw<RegraDeNegocioException>().WithMessage("*já está publicada*");
        publicarDesativada.Should().Throw<RegraDeNegocioException>().WithMessage("*desativada*");
        inexistente.Should().Throw<EntidadeNaoEncontradaException>().Which.Entidade.Should().Be("Peça do projeto");
    }

    [Test]
    public void DeveRecusarPublicarVersaoComPecaEmRascunhoDizendoQuaisEPublicarDepoisDePublicarAPeca()
    {
        var projeto = NovoProjeto();
        var elo = DefinirId(projeto.CriarPeca("Elo torcido", TipoPeca.Elo, null, null, null, null), 70);
        var fecho = DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(7, "Fecho lagosta", TipoPeca.Fecho)), 71);
        var v1 = DefinirId(projeto.CriarVersao("Base", null), 100);
        projeto.AdicionarItem(v1, elo, 12, null);
        projeto.AdicionarItem(v1, fecho, 1, null);

        var publicar = () => projeto.PublicarVersao(v1, Agora);

        publicar.Should().Throw<RegraDeNegocioException>().WithMessage("*rascunho: Elo torcido.*").Which.Message.Should().NotContain("Fecho");
        projeto.Versoes.Single().Status.Should().Be(StatusVersao.EmDesenvolvimento);

        projeto.PublicarPeca(elo);
        projeto.PublicarVersao(v1, Agora);
        projeto.Versoes.Single().Status.Should().Be(StatusVersao.Publicada);
    }

    [Test]
    public void DeveAceitarNaComposicaoSoPecaAtivaDoProjeto()
    {
        var projeto = NovoProjeto();
        var desativada = DefinirId(projeto.CriarPeca("Fecho", TipoPeca.Fecho, null, null, null, null), 70);
        projeto.AlterarPeca(desativada, "Fecho", TipoPeca.Fecho, null, null, null, null, ativo: false);
        var v1 = DefinirId(projeto.CriarVersao("Base", null), 100);

        var deOutroProjeto = () => projeto.AdicionarItem(v1, 999, 1, null);
        var inativa = () => projeto.AdicionarItem(v1, desativada, 1, null);

        deOutroProjeto.Should().Throw<EntidadeNaoEncontradaException>().Which.Entidade.Should().Be("Peça do projeto");
        inativa.Should().Throw<RegraDeNegocioException>().WithMessage("*desativada*");
    }

    [Test]
    public void DeveDuplicarVersaoApontandoParaAsMesmasPecasDoProjeto()
    {
        var projeto = NovoProjeto();
        var elo = DefinirId(projeto.ImportarPeca(PecaDaBiblioteca(7, "Elo oval", TipoPeca.Elo)), 70);
        var v1 = DefinirId(projeto.CriarVersao("Base", null), 100);
        projeto.AdicionarItem(v1, elo, 12, null);

        var copia = projeto.DuplicarVersao(v1, null);

        copia.Itens.Should().ContainSingle().Which.PecaDoProjetoId.Should().Be(elo);
        projeto.Pecas.Should().ContainSingle("duplicar a versão não copia as peças");
    }

    [Test]
    public void DeveVirarOutraPecaQuandoODesenhoDaPublicadaMuda()
    {
        var projeto = NovoProjeto();
        var copia = projeto.ImportarPeca(PecaDaBiblioteca(7, "Elo oval", TipoPeca.Elo));
        var id = DefinirId(copia, 31);
        projeto.DefinirValorDaPeca(id, Dinheiro.Criar(12m));

        var virou = projeto.AlterarDesenhoDaPeca(id);
        var deNovo = projeto.AlterarDesenhoDaPeca(id);

        virou.Should().BeTrue();
        deNovo.Should().BeFalse("em rascunho, mudar o desenho não muda nada");
        Obter(projeto, id).Should().BeEquivalentTo(new
        {
            Status = StatusPecaDoProjeto.Rascunho,
            PecaId = (long?)null,
            Nome = "Elo oval",
            Valor = (Dinheiro?)Dinheiro.Criar(12m),
        });
    }

    [Test]
    public void DeveRecusarAlterarODesenhoDePecaQueNaoEDoProjeto()
    {
        var projeto = NovoProjeto();

        var alterar = () => projeto.AlterarDesenhoDaPeca(99);

        alterar.Should().Throw<EntidadeNaoEncontradaException>();
    }

    private static Projeto NovoProjeto()
        => Projeto.Criar("Colar", null, null, new PrecosDoAtelie([], ParametrosDePreco.Criar(8m, Dinheiro.Criar(90m), 1.8m, 0m, Dinheiro.Criar(10m))), Agora);

    /// <summary>
    /// Pedra com material livre; os outros tipos com a liga 2 (Ouro 18k amarelo).
    /// </summary>
    private static Peca PecaDaBiblioteca(long id, string nome, TipoPeca tipo)
        => tipo == TipoPeca.Pedra
            ? ComId(Peca.Criar(nome, tipo, null, "Turmalina", Peso.EmGramas(0.35m), null), id)
            : ComId(Peca.Criar(nome, tipo, NovaLiga(2, "Ouro 18k amarelo"), null, Peso.EmGramas(0.35m), null), id);

    private static Liga NovaLiga(long id, string nome) => ComId(Liga.Criar(nome, Metal.Ouro, Ouro18k, 15.5m), id);

    private static PecaDoProjeto Obter(Projeto projeto, long id) => projeto.Pecas.Single(p => p.Id == id);

    private static long DefinirId<T>(T entidade, long id)
        where T : BaseEntity<long>
    {
        ComId(entidade, id);
        return id;
    }

    /// <summary>
    /// Fora do banco os Ids são 0; atribui como o EF faria ao salvar.
    /// </summary>
    private static T ComId<T>(T entidade, long id)
        where T : BaseEntity<long>
    {
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(entidade, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return entidade;
    }
}
