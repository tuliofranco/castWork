using System.Text;

using Castwork.Application.Assistente.Commands;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirMaoDeObra;
using Castwork.Application.Projetos.Commands.DefinirMetaisDaVersao;
using Castwork.Application.Projetos.Commands.RegistrarPesoReal;
using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.DomainService.Precificacao;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// O peso do metal pelo modelo 3D na <see cref="GetPrecoDaVersaoQuery"/>, com o medidor de teste (sem SolidCraft): a
/// versão sem 3D segue com o peso manual; com 3D, vale o peso medido (o manual fica guardado), pedra e apoio ficam fora,
/// corpo sem material deixa o preço incompleto, sobreposição de metais é aviso e a falha ao medir não derruba a leitura.
/// </summary>
public class PrecoPeloModelo3dTests : TesteDeIntegracao
{
    private const long Ouro18kAmarelo = 2;
    private const long Ouro18kBranco = 3;

    [Test]
    public async Task DeveUsarOPesoManualQuandoAVersaoNaoTem3dAsync()
    {
        var (_, versaoId) = await VersaoAsync();

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Calculo!.OrigemDoPeso.Should().Be(OrigemDoPeso.Manual);
        preco.Calculo.PesosManuaisGuardados.Should().BeEmpty();
        preco.Calculo.Metais.Single().Should().BeEquivalentTo(new { LigaId = Ouro18kAmarelo, PesoEstimado = 5m, Custo = 2592m });
        preco.Status.Should().Be(StatusDoPreco.Completo);
        AmbienteDeTeste.Medidor.Chamadas.Should().BeEmpty("sem arquivo 3D não há o que medir");
    }

    [Test]
    public async Task DeveUsarOPesoDoModeloNoLugarDoManualDeixandoPedraEApoioDeForaAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Medicao(
            Corpo("Aro", "Castwork liga 2", 2.4347343m),
            Corpo("Garra", "Castwork liga 2", 0.3895575m),
            Corpo("Friso", "Castwork liga 3", 0.5m),
            Corpo("Brilhante", MateriaisDoModelo.Pedra, 0.088m),
            Corpo("Guia", MateriaisDoModelo.Apoio, 1m));
        await AmbienteDeTeste.SendAsync(new RegistrarPesoRealCommand { ProjetoId = projetoId, VersaoId = versaoId, LigaId = Ouro18kAmarelo, PesoGramas = 3m });

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        var calculo = preco.Calculo!;
        calculo.OrigemDoPeso.Should().Be(OrigemDoPeso.Modelo);
        calculo.Metais.Select(m => (m.LigaId, m.Liga, m.PesoEstimado, m.PesoAFundir, m.Custo, m.PesoReal)).Should().Equal(
            (Ouro18kAmarelo, "Ouro 18k amarelo", 2.824m, 3.05m, 1464m, (decimal?)3m),
            (Ouro18kBranco, "Ouro 18k branco", 0.5m, 0.54m, 259.20m, null));
        calculo.PesosManuaisGuardados.Should().ContainSingle().Which.Should().BeEquivalentTo(new { LigaId = Ouro18kAmarelo, Liga = "Ouro 18k amarelo", Peso = 5m });
        preco.Status.Should().Be(StatusDoPreco.Completo);
        calculo.Avisos.Should().BeEmpty();

        var (arquivoMedido, materiais) = AmbienteDeTeste.Medidor.Chamadas.Should().ContainSingle().Subject;
        arquivoMedido.Should().Be(arquivoId);
        materiais.Select(m => m.Nome).Should().Contain(["Castwork liga 2", "Castwork liga 3", MateriaisDoModelo.Pedra, MateriaisDoModelo.Apoio]);
    }

    [Test]
    public async Task DeveDeixarIncompletoComCorpoSemMaterialAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Medicao(Corpo("Aro", "Castwork liga 2", 2.5m), Corpo("Garra", MateriaisDoModelo.SemMaterial, 0.03m));

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        var pendencia = preco.Calculo!.Pendencias.Should().ContainSingle().Subject;
        (pendencia.Tipo, pendencia.Descricao).Should().Be((TipoDePendencia.CorposSemMaterial, "1 corpo sem material no modelo 3D: Garra."));
        pendencia.Corpos.Should().Equal("Garra");
        preco.Calculo.Metais.Single().PesoEstimado.Should().Be(2.5m);
    }

    [Test]
    public async Task DeveAvisarDaSobreposicaoEntreCorposDeMetalAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Medicao(Corpo("Aro", "Castwork liga 2", 2.4m), Corpo("Garra", "Castwork liga 2", 0.4m)) with
        {
            Sobreposicoes = [new SobreposicaoMedida("Aro", "Garra", 19.398m)],
        };

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Status.Should().Be(StatusDoPreco.Completo);
        var aviso = preco.Calculo!.Avisos.Should().ContainSingle().Subject;
        (aviso.Tipo, aviso.Descricao).Should().Be((TipoDeAviso.CorposSobrepostos, "Aro e Garra se sobrepõem (19,4 mm³): a parte comum conta duas vezes no peso."));
        aviso.Corpos.Should().Equal("Aro", "Garra");
    }

    [Test]
    public async Task DeveMostrarOPrecoIncompletoQuandoNaoConsegueMedirAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = null;

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Status.Should().Be(StatusDoPreco.Incompleto);
        preco.Calculo!.Pendencias.Select(p => (p.Tipo, p.Descricao)).Should().Equal(
            (TipoDePendencia.ModeloNaoMedido, "Não foi possível medir o modelo 3D da versão: confira o arquivo e tente de novo."));
        preco.Calculo.Metais.Should().BeEmpty("o peso manual não é usado quando a versão tem 3D");
        preco.Calculo.CustoTotal.Should().Be(500m, "o resto do orçamento sai assim mesmo");
    }

    [Test]
    public async Task DeveVoltarAoPesoManualQuandoOArquivo3dEArquivadoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        await AmbienteDeTeste.SendAsync(new ArquivarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Modelo3dNoAssistente.Codigo,
            ArquivoId = arquivoId,
        });

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Calculo!.OrigemDoPeso.Should().Be(OrigemDoPeso.Manual);
        AmbienteDeTeste.Medidor.Chamadas.Should().BeEmpty();
    }

    private static MedicaoDoModelo Medicao(params CorpoMedido[] corpos) => new(corpos, []);

    private static CorpoMedido Corpo(string nome, string material, decimal gramas) => new(nome, material, gramas * 50, gramas, Fechado: true);

    /// <summary>
    /// O arquivo do Modelo 3D da versão, como a tela salva (o conteúdo não importa: quem mede é o medidor de teste).
    /// </summary>
    private static Task<long> Salvar3dAsync(long projetoId, long versaoId)
        => AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Modelo3dNoAssistente.Codigo,
            Conteudo = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"format":"solidcraft/51"}""")),
        });

    /// <summary>
    /// Ouro a R$ 640/g (18k: R$ 480/g), perda 8%, markup 1,8, sem taxas, arredondamento R$ 10; 5 g de ouro 18k amarelo
    /// digitados e mão de obra fixa de R$ 500.
    /// </summary>
    private async Task<(long ProjetoId, long VersaoId)> VersaoAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 8m, ValorHora = 90m, Markup = 1.8m, TaxasPercentual = 0m, ArredondarPara = 10m });
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        await AmbienteDeTeste.SendAsync(new DefinirMetaisDaVersaoCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Metais = [new MetalInformado { LigaId = Ouro18kAmarelo, PesoGramas = 5m }],
        });
        await AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.ValorFixo, Valor = 500m });
        return (projetoId, versaoId);
    }
}
