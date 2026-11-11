using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;
using Castwork.DomainService.Precificacao;

namespace Castwork.UnitTests.DomainService.Precificacao;

public class CalculadoraDePrecoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly MetalUsado Ouro18kAmarelo = Liga(2, "Ouro 18k amarelo", Metal.Ouro, 750, 5m);

    /// <summary>
    /// O exemplo da issue #14: 5 g de ouro 18k com 8% de perda, composição de R$ 800, 6 h × R$ 90, serviços de R$ 180,
    /// markup 1,8, taxas 6% e arredondamento de R$ 10.
    /// </summary>
    [Test]
    public void DeveCalcularOPrecoLinhaALinhaComoNoExemplo()
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo());

        var ouro = preco.Metais.Single();
        (ouro.PesoEstimado.Gramas, ouro.PesoAFundir.Gramas, ouro.CotacaoDoPuro!.Valor, ouro.ValorPorGrama!.Valor, ouro.Custo.Valor)
            .Should().Be((5m, 5.4m, 640m, 480m, 2592m));
        preco.CustoDaComposicao.Valor.Should().Be(800m);
        (preco.MaoDeObra.Tipo, preco.MaoDeObra.Horas, preco.MaoDeObra.ValorHora!.Valor, preco.MaoDeObra.Custo.Valor)
            .Should().Be((TipoMaoDeObra.PorHoras, 6m, 90m, 540m));
        preco.CustoDosServicos.Valor.Should().Be(180m);
        preco.CustoTotal.Valor.Should().Be(4112m);
        (preco.Markup, preco.MarkupDaVersao, preco.TaxasPercentual).Should().Be((1.8m, false, 6m));
        preco.PrecoSemArredondar.Valor.Should().Be(7845.70m, "4.112 × 1,8 × 1,06 = 7.845,696");
        preco.Preco.Valor.Should().Be(7850m, "arredondado para cima ao múltiplo de R$ 10");
        preco.Status.Should().Be(StatusDoPreco.Completo);
        preco.Pendencias.Should().BeEmpty();
        preco.Avisos.Should().BeEmpty();
    }

    [Test]
    public void DeveCobrarMaoDeObraPorValorFixoSemUsarOValorDaHora()
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo() with { MaoDeObra = MaoDeObra.Fixa(Dinheiro.Criar(700m)) });

        (preco.MaoDeObra.Tipo, preco.MaoDeObra.Horas, preco.MaoDeObra.ValorHora, preco.MaoDeObra.Custo.Valor)
            .Should().Be((TipoMaoDeObra.ValorFixo, (decimal?)null, (Dinheiro?)null, 700m));
        preco.CustoTotal.Valor.Should().Be(4272m);
    }

    [Test]
    public void DeveSomarDuasLigasCadaUmaComACotacaoDoSeuMetalEPureza()
    {
        var entrada = Exemplo() with
        {
            Metais =
            [
                Ouro18kAmarelo,
                Liga(3, "Ouro 18k branco", Metal.Ouro, 750, 2m),
                Liga(7, "Prata 925", Metal.Prata, 925, 10m),
            ],
        };

        var preco = CalculadoraDePreco.Calcular(entrada);

        // Branco: 2 g + 8% = 2,16 g × 480 = 1.036,80. Prata: 10,8 g × (7,00 × 0,925 = 6,475) = 69,93.
        preco.Metais.Select(m => (m.Liga, m.PesoAFundir.Gramas, m.ValorPorGrama!.Valor, m.Custo.Valor)).Should().Equal(
            ("Ouro 18k amarelo", 5.4m, 480m, 2592m),
            ("Ouro 18k branco", 2.16m, 480m, 1036.80m),
            ("Prata 925", 10.8m, 6.48m, 69.93m));
        preco.CustoDosMetais.Valor.Should().Be(3698.73m);
    }

    [TestCase(0, 5, 2400)]
    [TestCase(10, 5.5, 2640)]
    public void DeveAplicarAPerdaAoPesoAFundir(decimal perda, decimal pesoAFundir, decimal custo)
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo() with { Parametros = Parametros(perda: perda) });

        preco.Metais.Single().Should().BeEquivalentTo(new { PesoAFundir = Peso.EmGramas(pesoAFundir), Custo = Dinheiro.Criar(custo) });
    }

    [Test]
    public void DeveUsarOMarkupDaVersaoNoLugarDoDoProjeto()
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo() with { MarkupDaVersao = 2.5m });

        (preco.Markup, preco.MarkupDaVersao).Should().Be((2.5m, true));
        preco.PrecoSemArredondar.Valor.Should().Be(10896.80m, "4.112 × 2,5 × 1,06");
        preco.Preco.Valor.Should().Be(10900m);
    }

    [Test]
    public void DeveSomarAsTaxasSobreOPrecoENaoArredondarComArredondamentoZero()
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo() with { Parametros = Parametros(taxas: 10m, arredondarPara: 0m) });

        preco.Preco.Valor.Should().Be(8141.76m, "4.112 × 1,8 × 1,10, sem arredondamento");
    }

    [TestCase(7840, 7840)]
    [TestCase(7840.01, 7850)]
    public void DeveArredondarParaCimaAoMultiplo(decimal custoComposicao, decimal esperado)
    {
        // Só a composição, markup 1 e sem taxas: o preço sem arredondar é o próprio custo.
        var entrada = Exemplo() with
        {
            Parametros = Parametros(markup: 1m, taxas: 0m),
            Metais = [Ouro18kAmarelo],
            Cotacoes = [],
            Composicao = [new PecaUsada(1, "Pedra", 1, Dinheiro.Criar(custoComposicao))],
            MaoDeObra = null,
            Servicos = [],
        };

        CalculadoraDePreco.Calcular(entrada).Preco.Valor.Should().Be(esperado);
    }

    [Test]
    public void DeveListarAsPendenciasEDeixarOPrecoIncompleto()
    {
        var desativada = Liga(1000, "Ouro 22k antigo", Metal.Ouro, 916, 1m) with { LigaAtiva = false };
        var prata = Liga(7, "Prata 925", Metal.Prata, 925, 3m);
        var entrada = Exemplo() with
        {
            Parametros = ParametrosComHoraZero(),
            Cotacoes = [new CotacaoUsada(Metal.Ouro, Dinheiro.Criar(640m), Agora)],
            Metais = [Ouro18kAmarelo, desativada, prata],
            Composicao = [new PecaUsada(10, "Turmalina", 1, null), new PecaUsada(11, "Fecho", 2, Dinheiro.Criar(50m))],
        };

        var preco = CalculadoraDePreco.Calcular(entrada);

        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        preco.Pendencias.Select(p => (p.Tipo, p.PecaDoProjetoId, p.LigaId, p.Metal)).Should().BeEquivalentTo(new (TipoDePendencia, long?, long?, Metal?)[]
        {
            (TipoDePendencia.LigaDesativada, null, 1000, null),
            (TipoDePendencia.MetalSemCotacao, null, null, Metal.Prata),
            (TipoDePendencia.PecaSemValor, 10, null, null),
            (TipoDePendencia.ValorHoraADefinir, null, null, null),
        });
        preco.Pendencias.Select(p => p.Descricao).Should().Contain("Turmalina está sem valor no projeto.").And.Contain("O projeto não tem cotação de prata.");
        preco.Metais[^1].Should().BeEquivalentTo(new { CotacaoDoPuro = (Dinheiro?)null, ValorPorGrama = (Dinheiro?)null, Custo = Dinheiro.Zero });
        preco.CustoDaComposicao.Valor.Should().Be(100m, "a peça sem valor entra como zero");
        preco.Preco.Should().BeGreaterThan(Dinheiro.Zero, "o preço parcial sai assim mesmo");
    }

    [Test]
    public void DeveFicarIncompletoSemNenhumMetalComPeso()
    {
        var preco = CalculadoraDePreco.Calcular(Exemplo() with { Metais = [] });

        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        preco.Pendencias.Should().ContainSingle().Which.Tipo.Should().Be(TipoDePendencia.SemMetalComPeso);
    }

    [Test]
    public void DeveTerUmaPendenciaPorMetalSemCotacaoMesmoComDuasLigasDele()
    {
        var entrada = Exemplo() with
        {
            Cotacoes = [],
            Metais = [Ouro18kAmarelo, Liga(3, "Ouro 18k branco", Metal.Ouro, 750, 1m)],
        };

        CalculadoraDePreco.Calcular(entrada).Pendencias.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Tipo = TipoDePendencia.MetalSemCotacao, Metal = (Metal?)Metal.Ouro });
    }

    [TestCase(15, false)]
    [TestCase(16, true)]
    public void DeveAvisarSemBloquearQuandoACotacaoTemMaisDe15Dias(int dias, bool avisa)
    {
        var entrada = Exemplo() with
        {
            Cotacoes = [new CotacaoUsada(Metal.Ouro, Dinheiro.Criar(640m), Agora.AddDays(-dias))],
        };

        var preco = CalculadoraDePreco.Calcular(entrada);

        preco.Status.Should().Be(StatusDoPreco.Completo);
        if (avisa)
        {
            preco.Avisos.Should().ContainSingle().Which.Should().BeEquivalentTo(new
            {
                Tipo = TipoDeAviso.CotacaoAntiga,
                Descricao = "A cotação de ouro tem 16 dias: confira antes de orçar.",
                Metal = (Metal?)Metal.Ouro,
            });
        }
        else
        {
            preco.Avisos.Should().BeEmpty();
        }
    }

    [Test]
    public void DeveCompararOPesoRealComOEstimadoSemMudarOPreco()
    {
        var comPesoReal = Exemplo() with { Metais = [Ouro18kAmarelo with { PesoReal = Peso.EmGramas(5.2m) }] };

        var preco = CalculadoraDePreco.Calcular(comPesoReal);

        preco.Metais.Single().Should().BeEquivalentTo(new { PesoReal = Peso.EmGramas(5.2m), DiferencaDoPesoReal = (decimal?)4m });
        preco.Preco.Valor.Should().Be(7850m, "o preço usa o peso estimado");
    }

    [Test]
    public void DeveSerDeterministica()
    {
        var entrada = Exemplo();

        CalculadoraDePreco.Calcular(entrada).Should().BeEquivalentTo(CalculadoraDePreco.Calcular(entrada));
    }

    private static EntradaDoPreco Exemplo()
        => new(
            Parametros(),
            [new CotacaoUsada(Metal.Ouro, Dinheiro.Criar(640m), Agora.AddDays(-2)), new CotacaoUsada(Metal.Prata, Dinheiro.Criar(7m), Agora)],
            [Ouro18kAmarelo],
            [new PecaUsada(10, "Turmalina", 1, Dinheiro.Criar(500m)), new PecaUsada(11, "Fecho lagosta", 2, Dinheiro.Criar(150m))],
            MaoDeObra.PorHoras(6m),
            [new ServicoExterno("Fundição", Dinheiro.Criar(120m)), new ServicoExterno("Ródio", Dinheiro.Criar(60m))],
            MarkupDaVersao: null,
            Agora);

    private static ParametrosDePreco Parametros(decimal perda = 8m, decimal markup = 1.8m, decimal taxas = 6m, decimal arredondarPara = 10m)
        => ParametrosDePreco.Criar(perda, Dinheiro.Criar(90m), markup, taxas, Dinheiro.Criar(arredondarPara));

    /// <summary>
    /// Como o EF lê um projeto que herdou o valor da hora "a definir": zero, que a fábrica recusa.
    /// </summary>
    private static ParametrosDePreco ParametrosComHoraZero()
    {
        var parametros = Parametros();
        typeof(ParametrosDePreco).GetProperty(nameof(ParametrosDePreco.ValorHora))!.SetValue(parametros, Dinheiro.Zero);
        return parametros;
    }

    private static MetalUsado Liga(long ligaId, string liga, Metal metal, int teor, decimal gramas)
        => new(ligaId, liga, metal, TeorMetal.EmMilesimos(teor), LigaAtiva: true, Peso.EmGramas(gramas), PesoReal: null);
}
