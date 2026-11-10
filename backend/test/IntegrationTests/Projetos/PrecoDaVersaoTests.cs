using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Pecas.Commands.AlterarPeca;
using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Precificacao.Commands.CriarLiga;
using Castwork.Application.Precificacao.Commands.DesativarLiga;
using Castwork.Application.Projetos.Commands.AdicionarItem;
using Castwork.Application.Projetos.Commands.ArquivarVersao;
using Castwork.Application.Projetos.Commands.AtualizarPrecosDoProjeto;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirMaoDeObra;
using Castwork.Application.Projetos.Commands.DefinirMarkupDaVersao;
using Castwork.Application.Projetos.Commands.DefinirMetaisDaVersao;
using Castwork.Application.Projetos.Commands.DefinirServicos;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Application.Projetos.Commands.RegistrarPesoReal;
using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.DomainService.Precificacao;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using MediatR;

namespace Castwork.IntegrationTests.Projetos;

/// <summary>
/// Preço da versão pelo MediatR: os commands do orçamento (metais, mão de obra, serviços, markup e peso real), a query
/// com e sem <c>precificacao.ler</c>, as pendências e a versão publicada bloqueada.
/// </summary>
public class PrecoDaVersaoTests : TesteDeIntegracao
{
    private const long Ouro18kAmarelo = 2;
    private const long Ouro18kBranco = 3;
    private const long Prata925 = 7;

    [Test]
    public async Task DeveCalcularOPrecoDaVersaoComoNoExemploAsync()
    {
        var (_, versaoId) = await VersaoDoExemploAsync();

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        (preco.Preco, preco.Status).Should().Be((7850m, StatusDoPreco.Completo));
        var calculo = preco.Calculo!;
        calculo.Metais.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            LigaId = Ouro18kAmarelo,
            Liga = "Ouro 18k amarelo",
            Teor = 750,
            PesoEstimado = 5m,
            PesoAFundir = 5.4m,
            CotacaoDoPuro = (decimal?)640m,
            ValorPorGrama = (decimal?)480m,
            Custo = 2592m,
        });
        calculo.Composicao.Select(p => (p.Quantidade, p.CustoUnitario, p.Custo)).Should().Equal((1, (decimal?)500m, 500m), (2, 150m, 300m));
        (calculo.CustoDaComposicao, calculo.MaoDeObra.Custo, calculo.CustoDosServicos, calculo.CustoTotal).Should().Be((800m, 540m, 180m, 4112m));
        (calculo.MaoDeObra.Tipo, calculo.MaoDeObra.Horas, calculo.MaoDeObra.ValorHora).Should().Be((TipoMaoDeObra.PorHoras, 6m, 90m));
        calculo.Servicos.Select(s => (s.Nome, s.Valor)).Should().Equal(("Fundição", 120m), ("Ródio", 60m));
        (calculo.Markup, calculo.MarkupDaVersao, calculo.TaxasPercentual, calculo.PrecoSemArredondar).Should().Be((1.8m, (decimal?)null, 6m, 7845.70m));
        calculo.Pendencias.Should().BeEmpty();
        calculo.Avisos.Should().BeEmpty();
    }

    [Test]
    public async Task DeveCobrarValorFixoEUsarOMarkupDaVersaoAteVoltarAoDoProjetoAsync()
    {
        var (projetoId, versaoId) = await VersaoDoExemploAsync();

        await AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.ValorFixo, Valor = 700m });
        await AmbienteDeTeste.SendAsync(new DefinirMarkupDaVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId, Markup = 2m });
        var comMarkupDaVersao = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });
        await AmbienteDeTeste.SendAsync(new DefinirMarkupDaVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId, Markup = null });
        var comMarkupDoProjeto = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        comMarkupDaVersao.Calculo!.MaoDeObra.Should().BeEquivalentTo(new { Tipo = (TipoMaoDeObra?)TipoMaoDeObra.ValorFixo, Horas = (decimal?)null, Custo = 700m });
        (comMarkupDaVersao.Calculo.CustoTotal, comMarkupDaVersao.Calculo.Markup, comMarkupDaVersao.Calculo.MarkupDaVersao).Should().Be((4272m, 2m, (decimal?)2m));
        comMarkupDaVersao.Preco.Should().Be(9060m, "4.272 × 2 × 1,06 = 9.056,64");
        (comMarkupDoProjeto.Calculo!.Markup, comMarkupDoProjeto.Preco).Should().Be((1.8m, 8160m), "4.272 × 1,8 × 1,06 = 8.150,98");
    }

    [Test]
    public async Task DeveSomarDuasLigasCadaUmaComASuaCotacaoAsync()
    {
        var (projetoId, versaoId) = await VersaoDoExemploAsync();
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Prata, Valor = 7m });
        await AmbienteDeTeste.SendAsync(new AtualizarPrecosDoProjetoCommand { ProjetoId = projetoId });

        await AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (Ouro18kAmarelo, 5m), (Ouro18kBranco, 2m), (Prata925, 10m)));
        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Calculo!.Metais.Select(m => (m.Liga, m.ValorPorGrama, m.Custo)).Should().Equal(
            ("Ouro 18k amarelo", (decimal?)480m, 2592m),
            ("Ouro 18k branco", 480m, 1036.80m),
            ("Prata 925", 6.48m, 69.93m));
        preco.Calculo.CustoDosMetais.Should().Be(3698.73m);
        preco.Status.Should().Be(StatusDoPreco.Completo);
    }

    [Test]
    public async Task DeveMostrarAsPendenciasAteResolverCadaUmaAsync()
    {
        await AtualizarAtelieAsync(ouro: 640m);
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var pecas = Usar(new PecaBuilder());
        var semCusto = await pecas.BuildCommandAsync();
        var pecaId = await pecas.SendAsync(semCusto);
        await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaId = pecaId, Quantidade = 2 });
        var ligaAntiga = await AmbienteDeTeste.SendAsync(new CriarLigaCommand { Nome = "Ouro 22k antigo", Metal = Metal.Ouro, Teor = "916", Densidade = 17.8m });

        var vazia = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });
        await AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (ligaAntiga, 3m), (Prata925, 1m)));
        await AmbienteDeTeste.SendAsync(new DesativarLigaCommand { LigaId = ligaAntiga });
        var comLigas = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        await AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (Ouro18kAmarelo, 3m)));
        await AmbienteDeTeste.SendAsync(new AlterarPecaCommand
        {
            PecaId = pecaId,
            Nome = semCusto.Nome,
            Tipo = semCusto.Tipo,
            Material = semCusto.Material,
            Teor = semCusto.Teor,
            PesoGramas = semCusto.PesoGramas,
            Custo = 100m,
        });
        var resolvida = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        vazia.Status.Should().Be(StatusDoPreco.Incompleto);
        vazia.Calculo!.Pendencias.Select(p => (p.Tipo, p.PecaId)).Should()
            .BeEquivalentTo(new (TipoDePendencia, long?)[] { (TipoDePendencia.SemMetalComPeso, null), (TipoDePendencia.PecaSemCusto, pecaId) });
        comLigas.Calculo!.Pendencias.Select(p => (p.Tipo, p.LigaId, p.Metal)).Should().BeEquivalentTo(new (TipoDePendencia, long?, Metal?)[]
        {
            (TipoDePendencia.LigaDesativada, ligaAntiga, null),
            (TipoDePendencia.MetalSemCotacao, null, Metal.Prata),
            (TipoDePendencia.PecaSemCusto, null, null),
        });
        (resolvida.Status, resolvida.Calculo!.Pendencias.Count).Should().Be((StatusDoPreco.Completo, 0));
        resolvida.Calculo.CustoDaComposicao.Should().Be(200m);
    }

    [Test]
    public async Task DeveMostrarSoOPrecoEOStatusSemConsultarPrecificacaoAsync()
    {
        var (_, versaoId) = await VersaoDoExemploAsync();
        var soProjetos = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        (AmbienteDeTeste.Sessao.IsAdmin, AmbienteDeTeste.Sessao.PerfilId) = (false, soProjetos);

        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Should().BeEquivalentTo(new { VersaoId = versaoId, Preco = 7850m, Status = StatusDoPreco.Completo, Calculo = (CalculoDoPrecoDto?)null });
    }

    [TestCase(StatusVersao.Publicada)]
    [TestCase(StatusVersao.Arquivada)]
    public async Task DeveBloquearOOrcamentoDaVersaoPublicadaOuArquivadaMasRegistrarOPesoRealAsync(StatusVersao status)
    {
        var (projetoId, versaoId) = await VersaoDoExemploAsync();
        IRequest fechar = status == StatusVersao.Publicada
            ? new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId }
            : new ArquivarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId };
        await AmbienteDeTeste.SendAsync(fechar);

        var alteracoes = new Func<Task>[]
        {
            () => AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (Ouro18kAmarelo, 6m))),
            () => AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.PorHoras, Horas = 1m }),
            () => AmbienteDeTeste.SendAsync(new DefinirServicosCommand { ProjetoId = projetoId, VersaoId = versaoId }),
            () => AmbienteDeTeste.SendAsync(new DefinirMarkupDaVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId, Markup = 2m }),
        };
        foreach (var alterar in alteracoes)
        {
            await alterar.Should().ThrowAsync<RegraDeNegocioException>();
        }

        await AmbienteDeTeste.SendAsync(new RegistrarPesoRealCommand { ProjetoId = projetoId, VersaoId = versaoId, LigaId = Ouro18kAmarelo, PesoGramas = 5.2m });
        var preco = await AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

        preco.Calculo!.Metais.Single().Should().BeEquivalentTo(new { PesoEstimado = 5m, PesoReal = (decimal?)5.2m, DiferencaDoPesoReal = (decimal?)4m });
        preco.Preco.Should().Be(7850m, "o peso real não muda o preço");
    }

    [Test]
    public async Task DeveExigirEditarPrecificacaoParaOsCommandsDoOrcamentoAsync()
    {
        var (projetoId, versaoId) = await VersaoDoExemploAsync();
        var semEditar = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Editar, Regras.Precificacao.Consultar).BuildAsync();
        var editor = await Usar(new PerfilBuilder()).ComRegras(Regras.Precificacao.Editar).BuildAsync();
        var commands = new IRequest[]
        {
            Metais(projetoId, versaoId, (Ouro18kAmarelo, 5m)),
            new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.PorHoras, Horas = 6m },
            new DefinirServicosCommand { ProjetoId = projetoId, VersaoId = versaoId },
            new DefinirMarkupDaVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId },
            new RegistrarPesoRealCommand { ProjetoId = projetoId, VersaoId = versaoId, LigaId = Ouro18kAmarelo, PesoGramas = 5.1m },
        };
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = semEditar;
        foreach (var command in commands)
        {
            await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(command)).Should().ThrowAsync<ForbiddenAccessException>();
        }

        AmbienteDeTeste.Sessao.PerfilId = editor;
        foreach (var command in commands)
        {
            await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(command)).Should().NotThrowAsync();
        }
    }

    [Test]
    public async Task DeveValidarOsDadosDoOrcamentoAsync()
    {
        var (projetoId, versaoId) = await VersaoDoExemploAsync();

        var liga = () => AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (999_999, 1m)));
        var peso = () => AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (Ouro18kAmarelo, 1m), (Ouro18kBranco, 0m)));
        var horas = () => AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.PorHoras });
        var servicos = () => AmbienteDeTeste.SendAsync(new DefinirServicosCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Servicos = [new ServicoInformado { Nome = " ", Valor = 0m }],
        });
        var markup = () => AmbienteDeTeste.SendAsync(new DefinirMarkupDaVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId, Markup = 11m });
        var pesoReal = () => AmbienteDeTeste.SendAsync(new RegistrarPesoRealCommand { ProjetoId = projetoId, VersaoId = versaoId, LigaId = Prata925, PesoGramas = 1m });

        (await liga.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("metais[0].ligaId");
        (await peso.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("metais[1].pesoGramas");
        (await horas.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("horas");
        (await servicos.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("servicos[0].nome", "servicos[0].valor");
        (await markup.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("markup");
        await pesoReal.Should().ThrowAsync<EntidadeNaoEncontradaException>("a prata não está nos metais da versão");
    }

    private static async Task AtualizarAtelieAsync(decimal ouro)
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = ouro });
        await AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 8m, ValorHora = 90m, Markup = 1.8m, TaxasPercentual = 6m, ArredondarPara = 10m });
    }

    private static DefinirMetaisDaVersaoCommand Metais(long projetoId, long versaoId, params (long LigaId, decimal Gramas)[] metais)
        => new()
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Metais = [.. metais.Select(m => new MetalInformado { LigaId = m.LigaId, PesoGramas = m.Gramas })],
        };

    /// <summary>
    /// A versão do exemplo da issue: ouro a R$ 640/g, perda 8%, hora R$ 90, markup 1,8, taxas 6%, arredondamento R$ 10;
    /// 5 g de ouro 18k amarelo, composição de R$ 800 (1 × 500 + 2 × 150), 6 h e serviços de R$ 180.
    /// </summary>
    private async Task<(long ProjetoId, long VersaoId)> VersaoDoExemploAsync()
    {
        await AtualizarAtelieAsync(ouro: 640m);
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        var pecas = Usar(new PecaBuilder());
        foreach (var (custo, quantidade) in new[] { (500m, 1), (150m, 2) })
        {
            var peca = await pecas.BuildCommandAsync();
            peca.Custo = custo;
            var pecaId = await pecas.SendAsync(peca);
            await AmbienteDeTeste.SendAsync(new AdicionarItemCommand { ProjetoId = projetoId, VersaoId = versaoId, PecaId = pecaId, Quantidade = quantidade });
        }

        await AmbienteDeTeste.SendAsync(Metais(projetoId, versaoId, (Ouro18kAmarelo, 5m)));
        await AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.PorHoras, Horas = 6m });
        await AmbienteDeTeste.SendAsync(new DefinirServicosCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Servicos = [new ServicoInformado { Nome = "Fundição", Valor = 120m }, new ServicoInformado { Nome = "Ródio", Valor = 60m }],
        });
        return (projetoId, versaoId);
    }
}
