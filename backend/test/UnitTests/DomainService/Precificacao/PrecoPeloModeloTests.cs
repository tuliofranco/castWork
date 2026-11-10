using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;
using Castwork.DomainService.Precificacao;

namespace Castwork.UnitTests.DomainService.Precificacao;

/// <summary>
/// O peso do metal pelo modelo 3D: o somatório por liga a partir dos corpos medidos, a pendência de corpo sem material
/// (o material é obrigatório) e os avisos de corpos de metal sobrepostos e de corpo aberto.
/// </summary>
public class PrecoPeloModeloTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveSomarAMassaDosCorposDeMetalPorLigaDeixandoPedraApoioSemMaterialEAbertoDeFora()
    {
        var modelo = Medido(
            DeMetal("Aro", 2, 2.4347343m),
            DeMetal("Garra", 2, 0.3895575m),
            DeMetal("Detalhe", 3, 0.5m),
            Corpo("Brilhante", UsoDoCorpo.Pedra, 0.088m),
            Corpo("Guia", UsoDoCorpo.Apoio, 1m),
            Corpo("Pino", UsoDoCorpo.SemMaterial, 0.2m),
            new CorpoDoModelo("Aba", UsoDoCorpo.Metal, 2, MassaGramas: null, Fechado: false));

        modelo.PesoPorLiga().Should().Equal((2L, Peso.EmGramas(2.824m)), (3L, Peso.EmGramas(0.5m)));
    }

    [Test]
    public void DeveCalcularComOPesoDoModeloEMarcarAOrigem()
    {
        var modelo = Medido(DeMetal("Aro", 2, 5m), Corpo("Brilhante", UsoDoCorpo.Pedra, 0.1m));

        var preco = CalculadoraDePreco.Calcular(Entrada(5m, modelo));

        preco.OrigemDoPeso.Should().Be(OrigemDoPeso.Modelo);
        preco.Metais.Single().Custo.Valor.Should().Be(2592m, "5 g + 8% = 5,4 g × R$ 480,00");
        preco.Status.Should().Be(StatusDoPreco.Completo);
        preco.Pendencias.Should().BeEmpty();
        preco.Avisos.Should().BeEmpty();
    }

    [Test]
    public void DeveUsarOPesoManualSemModelo()
    {
        var preco = CalculadoraDePreco.Calcular(Entrada(5m, modelo: null));

        preco.OrigemDoPeso.Should().Be(OrigemDoPeso.Manual);
        preco.Status.Should().Be(StatusDoPreco.Completo);
    }

    [Test]
    public void DeveDeixarIncompletoComCorpoSemMaterial()
    {
        var modelo = Medido(DeMetal("Aro", 2, 5m), Corpo("Garra", UsoDoCorpo.SemMaterial, 0.4m), Corpo("Pino", UsoDoCorpo.SemMaterial, 0.1m));

        var preco = CalculadoraDePreco.Calcular(Entrada(5m, modelo));

        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        preco.Pendencias.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Tipo = TipoDePendencia.CorposSemMaterial,
            Descricao = "2 corpos sem material no modelo 3D: Garra, Pino.",
        });
        preco.Pendencias[0].Corpos.Should().Equal("Garra", "Pino");
        preco.Metais.Single().PesoEstimado.Should().Be(Peso.EmGramas(5m), "o corpo sem material não entra no peso");
    }

    [Test]
    public void DeveAvisarSoDaSobreposicaoEntreCorposDeMetal()
    {
        var modelo = Medido(DeMetal("Aro", 2, 5m), DeMetal("Garra", 2, 0.4m), Corpo("Brilhante", UsoDoCorpo.Pedra, 0.1m)) with
        {
            Sobreposicoes =
            [
                new SobreposicaoDeCorpos("Aro", "Garra", 19.398m),
                new SobreposicaoDeCorpos("Garra", "Brilhante", 2m),
            ],
        };

        var preco = CalculadoraDePreco.Calcular(Entrada(5.4m, modelo));

        preco.Status.Should().Be(StatusDoPreco.Completo, "sobreposição é aviso, não pendência");
        preco.Avisos.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Tipo = TipoDeAviso.CorposSobrepostos,
            Descricao = "Aro e Garra se sobrepõem (19,4 mm³): a parte comum conta duas vezes no peso.",
        });
        preco.Avisos[0].Corpos.Should().Equal("Aro", "Garra");
    }

    [Test]
    public void DeveAvisarDoCorpoAbertoQueContariaNoPeso()
    {
        var modelo = Medido(
            DeMetal("Aro", 2, 5m),
            new CorpoDoModelo("Aba", UsoDoCorpo.Metal, 2, MassaGramas: null, Fechado: false),
            new CorpoDoModelo("Guia", UsoDoCorpo.Apoio, null, MassaGramas: null, Fechado: false));

        var preco = CalculadoraDePreco.Calcular(Entrada(5m, modelo));

        preco.Avisos.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Tipo = TipoDeAviso.CorpoAberto,
            Descricao = "O corpo Aba está aberto: o volume não é confiável e ele fica fora do peso.",
        });
        preco.Avisos[0].Corpos.Should().Equal("Aba");
    }

    [Test]
    public void DevePedirOMetalQuandoOModeloNaoTemCorpoDeMetal()
    {
        var modelo = Medido(Corpo("Brilhante", UsoDoCorpo.Pedra, 0.1m));

        var preco = CalculadoraDePreco.Calcular(Entrada(null, modelo));

        preco.Pendencias.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Tipo = TipoDePendencia.SemMetalComPeso,
            Descricao = "O modelo 3D não tem nenhum corpo de metal com peso.",
        });
    }

    [Test]
    public void DeveDeixarIncompletoQuandoNaoFoiPossivelMedir()
    {
        var preco = CalculadoraDePreco.Calcular(Entrada(null, ModeloMedido.NaoMedido));

        preco.OrigemDoPeso.Should().Be(OrigemDoPeso.Modelo);
        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        preco.Pendencias.Select(p => p.Tipo).Should().Equal(TipoDePendencia.ModeloNaoMedido);
    }

    private static ModeloMedido Medido(params CorpoDoModelo[] corpos) => new(Medido: true, corpos, []);

    private static CorpoDoModelo DeMetal(string nome, long ligaId, decimal gramas) => new(nome, UsoDoCorpo.Metal, ligaId, gramas, Fechado: true);

    private static CorpoDoModelo Corpo(string nome, UsoDoCorpo uso, decimal gramas) => new(nome, uso, null, gramas, Fechado: true);

    /// <summary>
    /// Ouro a R$ 640,00 (18k: R$ 480,00/g), 8% de perda, mão de obra fixa: os metais vêm do peso informado (do modelo,
    /// na query; aqui, direto).
    /// </summary>
    private static EntradaDoPreco Entrada(decimal? gramasDeOuro18k, ModeloMedido? modelo)
    {
        IReadOnlyList<MetalUsado> metais = gramasDeOuro18k is null
            ? []
            : [new MetalUsado(2, "Ouro 18k amarelo", Metal.Ouro, TeorMetal.EmMilesimos(750), LigaAtiva: true, Peso.EmGramas(gramasDeOuro18k.Value), PesoReal: null)];
        return new(
            ParametrosDePreco.Criar(8m, Dinheiro.Criar(90m), 1.8m, 0m, Dinheiro.Criar(10m)),
            [new CotacaoUsada(Metal.Ouro, Dinheiro.Criar(640m), Agora)],
            metais,
            [],
            MaoDeObra.Fixa(Dinheiro.Criar(500m)),
            [],
            MarkupDaVersao: null,
            Agora,
            modelo);
    }
}
