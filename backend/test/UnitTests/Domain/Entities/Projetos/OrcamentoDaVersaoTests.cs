using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.Entities.Projetos;

/// <summary>
/// Dados do orçamento da versão: metais com peso, mão de obra, serviços, markup e peso real.
/// </summary>
public class OrcamentoDaVersaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveDefinirOOrcamentoDaVersaoEmDesenvolvimento()
    {
        var (projeto, v1) = ProjetoComVersao();

        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(5m)), (3, Peso.EmGramas(1.25m))]);
        projeto.DefinirMaoDeObraDaVersao(v1, MaoDeObra.PorHoras(6m));
        projeto.DefinirServicosDaVersao(v1, [new ServicoExterno(" Fundição ", Dinheiro.Criar(120m))]);
        projeto.DefinirMarkupDaVersao(v1, 2.255m);

        var versao = Versao(projeto, v1);
        versao.Metais.Select(m => (m.LigaId, m.PesoEstimado.Gramas, m.PesoReal)).Should().Equal((2L, 5m, (Peso?)null), (3L, 1.25m, null));
        versao.MaoDeObra.Should().Be(MaoDeObra.PorHoras(6m));
        versao.Servicos.Should().Equal(new ServicoExterno("Fundição", Dinheiro.Criar(120m)));
        versao.Markup.Should().Be(2.26m);
    }

    [Test]
    public void DeveManterOPesoRealDaLigaQueContinuaAoRedefinirOsMetais()
    {
        var (projeto, v1) = ProjetoComVersao();
        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(5m)), (3, Peso.EmGramas(1m))]);
        projeto.RegistrarPesoReal(v1, 2, Peso.EmGramas(5.2m));

        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(4.8m)), (7, Peso.EmGramas(2m))]);

        Versao(projeto, v1).Metais.Select(m => (m.LigaId, m.PesoEstimado.Gramas, m.PesoReal?.Gramas))
            .Should().Equal((2L, 4.8m, (decimal?)5.2m), (7L, 2m, null));
    }

    [Test]
    public void DeveRecusarLigaRepetidaPesoForaDaFaixaEMarkupInvalido()
    {
        var (projeto, v1) = ProjetoComVersao();

        var repetida = () => projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(1m)), (2, Peso.EmGramas(2m))]);
        var semPeso = () => projeto.DefinirMetaisDaVersao(v1, [(2, Peso.Zero)]);
        var markup = () => projeto.DefinirMarkupDaVersao(v1, 0m);

        repetida.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Metais");
        semPeso.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("PesoEstimado");
        markup.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Markup");
    }

    [Test]
    public void DeveValidarMaoDeObraEServico()
    {
        var horas = () => MaoDeObra.PorHoras(0m);
        var fixa = () => MaoDeObra.Fixa(Dinheiro.Zero);
        var servico = () => new ServicoExterno("  ", Dinheiro.Criar(10m));

        horas.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Horas");
        fixa.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("ValorFixo");
        servico.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Nome");
        MaoDeObra.PorHoras(6m).Custo(Dinheiro.Criar(90m)).Should().Be(Dinheiro.Criar(540m));
        MaoDeObra.Fixa(Dinheiro.Criar(700m)).Custo(Dinheiro.Criar(90m)).Should().Be(Dinheiro.Criar(700m));
    }

    [Test]
    public void DeveVoltarAoMarkupDoProjetoComMarkupNulo()
    {
        var (projeto, v1) = ProjetoComVersao();
        projeto.DefinirMarkupDaVersao(v1, 2m);

        projeto.DefinirMarkupDaVersao(v1, null);

        Versao(projeto, v1).Markup.Should().BeNull();
    }

    [TestCase(StatusVersao.Publicada, "*publicada*")]
    [TestCase(StatusVersao.Arquivada, "*arquivada*")]
    public void DeveBloquearOOrcamentoDaVersaoPublicadaOuArquivadaMasAceitarOPesoReal(StatusVersao status, string mensagem)
    {
        var (projeto, v1) = ProjetoComVersao();
        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(5m))]);
        if (status == StatusVersao.Publicada)
        {
            projeto.PublicarVersao(v1, Agora);
        }
        else
        {
            projeto.ArquivarVersao(v1);
        }

        var alteracoes = new Action[]
        {
            () => projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(6m))]),
            () => projeto.DefinirMaoDeObraDaVersao(v1, MaoDeObra.PorHoras(1m)),
            () => projeto.DefinirServicosDaVersao(v1, []),
            () => projeto.DefinirMarkupDaVersao(v1, 2m),
        };

        alteracoes.Should().AllSatisfy(a => a.Should().Throw<RegraDeNegocioException>().WithMessage(mensagem));
        projeto.RegistrarPesoReal(v1, 2, Peso.EmGramas(5.2m));
        Versao(projeto, v1).Metais.Single().PesoReal.Should().Be(Peso.EmGramas(5.2m));
    }

    [Test]
    public void DeveApagarOPesoRealERecusarLigaForaDaVersao()
    {
        var (projeto, v1) = ProjetoComVersao();
        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(5m))]);
        projeto.RegistrarPesoReal(v1, 2, Peso.EmGramas(5.2m));

        projeto.RegistrarPesoReal(v1, 2, null);
        var foraDaVersao = () => projeto.RegistrarPesoReal(v1, 3, Peso.EmGramas(1m));

        Versao(projeto, v1).Metais.Single().PesoReal.Should().BeNull();
        foraDaVersao.Should().Throw<EntidadeNaoEncontradaException>();
    }

    [Test]
    public void DeveDuplicarCopiandoOOrcamentoSemOPesoReal()
    {
        var (projeto, v1) = ProjetoComVersao();
        projeto.DefinirMetaisDaVersao(v1, [(2, Peso.EmGramas(5m))]);
        projeto.DefinirMaoDeObraDaVersao(v1, MaoDeObra.Fixa(Dinheiro.Criar(700m)));
        projeto.DefinirServicosDaVersao(v1, [new ServicoExterno("Ródio", Dinheiro.Criar(60m))]);
        projeto.DefinirMarkupDaVersao(v1, 2m);
        projeto.PublicarVersao(v1, Agora);
        projeto.RegistrarPesoReal(v1, 2, Peso.EmGramas(5.2m));

        var copia = projeto.DuplicarVersao(v1, nome: null);

        copia.Metais.Should().Equal(new MetalDaVersao(2, Peso.EmGramas(5m), null));
        copia.Should().BeEquivalentTo(new { MaoDeObra = MaoDeObra.Fixa(Dinheiro.Criar(700m)), Markup = (decimal?)2m });
        copia.Servicos.Should().Equal(new ServicoExterno("Ródio", Dinheiro.Criar(60m)));
    }

    private static (Projeto Projeto, long VersaoId) ProjetoComVersao()
    {
        var atelie = new PrecosDoAtelie([], ParametrosDePreco.Criar(8m, Dinheiro.Criar(90m), 1.8m, 0m, Dinheiro.Criar(10m)));
        var projeto = Projeto.Criar("Colar", null, null, atelie, Agora);
        var versao = projeto.CriarVersao("Base", null);
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(versao, 100L, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return (projeto, 100);
    }

    private static VersaoProjeto Versao(Projeto projeto, long id) => projeto.Versoes.Single(v => v.Id == id);
}
