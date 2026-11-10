using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Pecas;

/// <summary>
/// Peça da biblioteca, com o material pela liga do ateliê (cópia do nome e do teor) ou, na pedra, texto livre.
/// </summary>
public class PecaTests
{
    [Test]
    public void DeveCriarPecaAtivaComDadosNormalizadosECopiaDaLiga()
    {
        var peca = Peca.Criar("  Elo oval ", TipoPeca.Elo, NovaLiga(2, "Ouro 18k amarelo", TeorMetal.Ouro18k), null, Peso.EmGramas(0.35m), "  ");

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
        });
    }

    [Test]
    public void DeveGuardarUmDesenhoPorFerramentaSubstituindoOAnterior()
    {
        var peca = Peca.Criar("Elo oval", TipoPeca.Elo, null, null, null, null);
        var em = new DateTimeOffset(2026, 11, 10, 15, 0, 0, TimeSpan.Zero);

        peca.GuardarDesenho("desenho-2d", [1, 2], em);
        peca.GuardarDesenho("modelo-3d", [3], em);
        peca.GuardarDesenho("desenho-2d", [4], em.AddMinutes(1));

        peca.Desenhos.Select(d => (d.Ferramenta, d.Conteudo.Length, d.CopiadoEm)).Should().BeEquivalentTo([("modelo-3d", 1, em), ("desenho-2d", 1, em.AddMinutes(1))]);
    }

    [Test]
    public void DeveRecusarDesenhoVazioOuSemFerramenta()
    {
        var peca = Peca.Criar("Elo oval", TipoPeca.Elo, null, null, null, null);

        var vazio = () => peca.GuardarDesenho("desenho-2d", [], DateTimeOffset.UnixEpoch);
        var semFerramenta = () => peca.GuardarDesenho(" ", [1], DateTimeOffset.UnixEpoch);

        vazio.Should().Throw<ValorInvalidoException>();
        semFerramenta.Should().Throw<ValorInvalidoException>();
        peca.Desenhos.Should().BeEmpty();
    }

    [Test]
    public void DeveCriarPedraComMaterialLivreSemLigaESemTeor()
    {
        var peca = Peca.Criar("Turmalina 4 mm", TipoPeca.Pedra, null, " Turmalina ", null, null);

        peca.Should().BeEquivalentTo(new { LigaId = (long?)null, Material = "Turmalina", Teor = (TeorMetal?)null });
    }

    [Test]
    public void DeveCriarPecaDeMetalSemLiga()
    {
        var peca = Peca.Criar("Fecho lagosta", TipoPeca.Fecho, null, null, null, null);

        peca.Should().BeEquivalentTo(new { LigaId = (long?)null, Material = (string?)null, Teor = (TeorMetal?)null });
    }

    [Test]
    public void DeveRecusarLigaNaPedraMaterialLivreNoMetalELigaDesativada()
    {
        var desativada = NovaLiga(5, "Ouro 14k amarelo", TeorMetal.Ouro14k);
        desativada.Desativar();

        var ligaNaPedra = () => Peca.Criar("Turmalina", TipoPeca.Pedra, NovaLiga(2, "Ouro 18k amarelo", TeorMetal.Ouro18k), null, null, null);
        var textoNoMetal = () => Peca.Criar("Elo", TipoPeca.Elo, null, "Ouro amarelo", null, null);
        var ligaDesativada = () => Peca.Criar("Elo", TipoPeca.Elo, desativada, null, null, null);

        ligaNaPedra.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Liga");
        textoNoMetal.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Material");
        ligaDesativada.Should().Throw<RegraDeNegocioException>().WithMessage("*Ouro 14k amarelo está desativada*");
    }

    [Test]
    public void DeveManterACopiaAoAtualizarComAMesmaLigaMesmoEditadaOuDesativada()
    {
        var liga = NovaLiga(2, "Ouro 18k amarelo", TeorMetal.Ouro18k);
        var peca = Peca.Criar("Elo oval", TipoPeca.Elo, liga, null, null, null);
        liga.Alterar("Ouro 18k amarelo (novo)", Metal.Ouro, TeorMetal.EmMilesimos(760), 15.6m);
        liga.Desativar();

        peca.Atualizar("Elo oval grande", TipoPeca.Elo, liga, null, null, null);
        var trocarParaDesativada = () => peca.Atualizar("Elo oval grande", TipoPeca.Elo, DesativadaComId(5), null, null, null);

        peca.Should().BeEquivalentTo(new { Nome = "Elo oval grande", LigaId = (long?)2, Material = "Ouro 18k amarelo", Teor = TeorMetal.Ouro18k });
        trocarParaDesativada.Should().Throw<RegraDeNegocioException>().WithMessage("*desativada*");
    }

    [Test]
    public void DeveTrocarDeMetalParaPedraSemLevarALiga()
    {
        var peca = Peca.Criar("Peça", TipoPeca.Elo, NovaLiga(2, "Ouro 18k amarelo", TeorMetal.Ouro18k), null, null, null);

        peca.Atualizar("Peça", TipoPeca.Pedra, null, "Turmalina", null, null);

        peca.Should().BeEquivalentTo(new { LigaId = (long?)null, Material = "Turmalina", Teor = (TeorMetal?)null });
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

    private static Liga DesativadaComId(long id)
    {
        var liga = NovaLiga(id, "Ouro 14k amarelo", TeorMetal.Ouro14k);
        liga.Desativar();
        return liga;
    }

    /// <summary>
    /// Fora do banco o Id é 0; atribui como o EF faria ao salvar.
    /// </summary>
    private static Liga NovaLiga(long id, string nome, TeorMetal teor)
    {
        var liga = Liga.Criar(nome, Metal.Ouro, teor, 15.5m);
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(liga, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return liga;
    }
}
