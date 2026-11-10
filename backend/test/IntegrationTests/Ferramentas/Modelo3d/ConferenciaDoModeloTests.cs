using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Application.Ferramentas.Modelo3d.Commands;
using Castwork.Application.Ferramentas.Modelo3d.Commands.DefinirMaterialDosCorpos;
using Castwork.Application.Ferramentas.Modelo3d.Commands.UnirCorposSobrepostos;
using Castwork.Application.Ferramentas.Modelo3d.Queries.GetConferenciaDoModelo;
using Castwork.Application.Precificacao.Commands.AtualizarCotacao;
using Castwork.Application.Precificacao.Commands.AtualizarParametrosDePreco;
using Castwork.Application.Projetos.Commands.ArquivarVersao;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Application.Projetos.Commands.DefinirMaoDeObra;
using Castwork.Application.Projetos.Commands.PublicarVersao;
using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;
using Castwork.DomainService.Precificacao;
using Castwork.IntegrationTests.GraphQl;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using MediatR;

namespace Castwork.IntegrationTests.Ferramentas.Modelo3d;

/// <summary>
/// Conferência de peso e materiais (etapa 7) com o medidor e o editor de teste (sem SolidCraft): a leitura dos corpos, o
/// material definido e os corpos unidos viram uma nova versão do arquivo (desfazível), com as permissões, a versão
/// publicada ou arquivada bloqueada e o modelo que mudou no meio do caminho recusado.
/// </summary>
public class ConferenciaDoModeloTests : TesteDeIntegracao
{
    private const string Ouro18kAmarelo = "Castwork liga 2";
    private const string Ouro18kBranco = "Castwork liga 3";
    private const string ConteudoDo3d = """{"format":"solidcraft/51"}""";

    private static readonly MedicaoDoModelo Anel = new(
        [
            new CorpoMedido("Aro", Ouro18kAmarelo, 322.581m, 5.0000055m, Fechado: true),
            new CorpoMedido("Chaton", MateriaisDoModelo.SemMaterial, 41.2m, 0.0531m, Fechado: true),
            new CorpoMedido("Pedra central", MateriaisDoModelo.Pedra, 28m, 0.098m, Fechado: true),
            new CorpoMedido("Guia", MateriaisDoModelo.Apoio, null, null, Fechado: false),
        ],
        [new SobreposicaoMedida("Aro", "Chaton", 0.4m)]);

    [Test]
    public async Task DeveDizerQueAVersaoNaoTem3dAsync()
    {
        var (_, versaoId) = await VersaoAsync();

        var conferencia = await AmbienteDeTeste.SendAsync(new GetConferenciaDoModeloQuery { VersaoId = versaoId });

        conferencia.Should().BeEquivalentTo(new { VersaoId = versaoId, ArquivoId = (long?)null, Medido = false, Alteravel = true });
        conferencia.Corpos.Should().BeEmpty();
    }

    [Test]
    public async Task DeveListarOsCorposComMaterialVolumeEPesoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;

        var conferencia = await AmbienteDeTeste.SendAsync(new GetConferenciaDoModeloQuery { VersaoId = versaoId });

        conferencia.Should().BeEquivalentTo(new { ArquivoId = (long?)arquivoId, Medido = true, Alteravel = true, PesoDoMetal = 5m });
        conferencia.PesosPorLiga.Should().ContainSingle().Which.Should().BeEquivalentTo(new { LigaId = 2L, Liga = "Ouro 18k amarelo", Peso = 5m });
        conferencia.Corpos.Select(c => (c.Nome, c.Uso, c.Liga, c.VolumeMm3, c.MassaGramas, c.ContaNoPeso)).Should().Equal(
            ("Aro", UsoDoCorpo.Metal, "Ouro 18k amarelo", 322.581m, 5m, true),
            ("Chaton", UsoDoCorpo.SemMaterial, null, 41.2m, 0.053m, false),
            ("Pedra central", UsoDoCorpo.Pedra, null, 28m, 0.098m, false),
            ("Guia", UsoDoCorpo.Apoio, null, null, null, false));
        conferencia.Sobreposicoes.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { CorpoA = "Aro", CorpoB = "Chaton", VolumeMm3 = 0.4m, DeMetal = false, PodeUnir = false });
        conferencia.Materiais.Select(m => (m.Nome, m.Rotulo, m.Uso)).Should().Contain(
            [(Ouro18kAmarelo, "Ouro 18k amarelo", UsoDoCorpo.Metal), (MateriaisDoModelo.Pedra, MateriaisDoModelo.Pedra, UsoDoCorpo.Pedra), (MateriaisDoModelo.Apoio, MateriaisDoModelo.Apoio, UsoDoCorpo.Apoio)]);
    }

    [Test]
    public async Task DeveDefinirOMaterialNumaNovaVersaoDoArquivoQueSeDesfazAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;
        var antes = await PrecoAsync(versaoId);

        var alterado = await AmbienteDeTeste.SendAsync(new DefinirMaterialDosCorposCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            ArquivoId = arquivoId,
            Corpos = [new MaterialInformado { Corpo = "Chaton", Material = Ouro18kAmarelo }, new MaterialInformado { Corpo = "Guia", Material = MateriaisDoModelo.Apoio }],
        });

        alterado.ArquivoAnteriorId.Should().Be(arquivoId);
        alterado.ArquivoId.Should().BeGreaterThan(arquivoId);
        var (arquivo, materiais, alteracao) = AmbienteDeTeste.Editor.Chamadas.Should().ContainSingle().Subject;
        Encoding.UTF8.GetString(arquivo).Should().Be(ConteudoDo3d, "o editor recebe o arquivo atual");
        materiais.Select(m => m.Nome).Should().Contain([Ouro18kAmarelo, MateriaisDoModelo.Pedra, MateriaisDoModelo.Apoio]);
        alteracao.Should().BeEquivalentTo(new DefinicaoDeMateriais([new MaterialDoCorpo("Chaton", Ouro18kAmarelo), new MaterialDoCorpo("Guia", MateriaisDoModelo.Apoio)]));

        var historico = await HistoricoAsync(projetoId, versaoId);
        historico.Select(v => (v.Id, v.Origem)).Should().Equal((alterado.ArquivoId, OrigemArquivo.Conferencia), (arquivoId, OrigemArquivo.Joalheiro));

        // O preço mede o arquivo novo (a medição guardada é pelo id do arquivo): com o material, fica completo.
        antes.Status.Should().Be(StatusDoPreco.Incompleto);
        AmbienteDeTeste.Medidor.Medicao = Anel with { Corpos = [.. Anel.Corpos.Select(c => c.Nome == "Chaton" ? c with { Material = Ouro18kAmarelo, MassaGramas = 0.6386m } : c)] };
        var depois = await PrecoAsync(versaoId);
        depois.Status.Should().Be(StatusDoPreco.Completo);
        depois.Calculo!.Metais.Single().PesoEstimado.Should().Be(5.639m);
        AmbienteDeTeste.Medidor.Chamadas[^1].ArquivoId.Should().Be(alterado.ArquivoId);

        // Desfazer: o arquivo de antes volta a ser o atual (uma cópia, o histórico só cresce).
        var restaurado = await AmbienteDeTeste.SendAsync(new RestaurarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Modelo3dNoAssistente.Codigo,
            ArquivoId = alterado.ArquivoAnteriorId,
        });
        var atual = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Modelo3dNoAssistente.Codigo });
        atual!.Id.Should().Be(restaurado!.Value);
        Encoding.UTF8.GetString(atual.Conteudo).Should().Be(ConteudoDo3d);
    }

    [Test]
    public async Task DeveUnirCorposDeMetalDaMesmaLigaQueSeSobrepoemAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = new MedicaoDoModelo(
            [
                new CorpoMedido("Aro", Ouro18kAmarelo, 322.6m, 5m, Fechado: true),
                new CorpoMedido("Chaton", Ouro18kAmarelo, 41.2m, 0.6386m, Fechado: true),
                new CorpoMedido("Friso", Ouro18kBranco, 10m, 0.15m, Fechado: true),
            ],
            [new SobreposicaoMedida("Aro", "Chaton", 0.4m), new SobreposicaoMedida("Aro", "Friso", 0.2m)]);
        var conferencia = await AmbienteDeTeste.SendAsync(new GetConferenciaDoModeloQuery { VersaoId = versaoId });
        conferencia.Sobreposicoes.Select(s => (s.CorpoB, s.DeMetal, s.PodeUnir)).Should().Equal(("Chaton", true, true), ("Friso", true, false));

        var alterado = await AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Chaton"));

        AmbienteDeTeste.Editor.Chamadas.Should().ContainSingle().Which.Alteracao.Should().BeEquivalentTo(new UniaoDeCorpos("Aro", ["Chaton"]));
        (await HistoricoAsync(projetoId, versaoId))[0].Should().BeEquivalentTo(new { Id = alterado.ArquivoId, Origem = OrigemArquivo.Conferencia });

        // Depois de unir, a medição do arquivo novo não tem mais a sobreposição: o aviso some e o peso não conta em dobro.
        AmbienteDeTeste.Medidor.Medicao = new MedicaoDoModelo([new CorpoMedido("Aro", Ouro18kAmarelo, 363.4m, 5.6327m, Fechado: true)], []);
        var preco = await PrecoAsync(versaoId);
        preco.Calculo!.Avisos.Should().BeEmpty();
        preco.Calculo.Metais.Single().PesoEstimado.Should().Be(5.633m);
    }

    [Test]
    public async Task DeveRecusarUnirLigasDiferentesOuCorposQueNaoSeSobrepoemAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = new MedicaoDoModelo(
            [
                new CorpoMedido("Aro", Ouro18kAmarelo, 322.6m, 5m, Fechado: true),
                new CorpoMedido("Friso", Ouro18kBranco, 10m, 0.15m, Fechado: true),
                new CorpoMedido("Pino", Ouro18kAmarelo, 6m, 0.09m, Fechado: true),
                new CorpoMedido("Pedra central", MateriaisDoModelo.Pedra, 28m, 0.098m, Fechado: true),
            ],
            [new SobreposicaoMedida("Aro", "Friso", 0.2m), new SobreposicaoMedida("Aro", "Pedra central", 0.1m)]);

        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Friso")))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("Só dá para unir corpos de metal da mesma liga.");
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Pedra central")))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("Só dá para unir corpos de metal da mesma liga.");
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Pino")))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("Esses corpos não se sobrepõem: não há o que unir.");
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro")))
            .Should().ThrowAsync<ValidationException>();
        AmbienteDeTeste.Editor.Chamadas.Should().BeEmpty();
    }

    [Test]
    public async Task DeveRecusarCorpoForaDoModeloMaterialDesconhecidoEModeloQueMudouAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;

        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Garra", Ouro18kAmarelo)))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("O corpo \"Garra\" não está no modelo 3D. Confira de novo.");
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", MateriaisDoModelo.SemMaterial)))
            .Should().ThrowAsync<ValidationException>();
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", "Castwork liga 999")))
            .Should().ThrowAsync<ValidationException>("liga que não existe (ou desativada) não é material do ateliê");

        var novoId = await Salvar3dAsync(projetoId, versaoId);
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().ThrowAsync<RegraDeNegocioException>().WithMessage("O modelo 3D mudou desde que a conferência foi aberta. Confira de novo.");
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, novoId, "Chaton", Ouro18kAmarelo)))
            .Should().NotThrowAsync();
    }

    [TestCase(StatusVersao.Publicada)]
    [TestCase(StatusVersao.Arquivada)]
    public async Task DeveBloquearAVersaoPublicadaOuArquivadaAsync(StatusVersao status)
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;
        IRequest fechar = status == StatusVersao.Publicada
            ? new PublicarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId }
            : new ArquivarVersaoCommand { ProjetoId = projetoId, VersaoId = versaoId };
        await AmbienteDeTeste.SendAsync(fechar);

        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().ThrowAsync<RegraDeNegocioException>();
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Chaton")))
            .Should().ThrowAsync<RegraDeNegocioException>();

        (await AmbienteDeTeste.SendAsync(new GetConferenciaDoModeloQuery { VersaoId = versaoId })).Alteravel.Should().BeFalse("a conferência fica só leitura");
        AmbienteDeTeste.Editor.Chamadas.Should().BeEmpty();
        (await HistoricoAsync(projetoId, versaoId)).Should().ContainSingle("nada foi gravado");
    }

    [Test]
    public async Task DeveExigirEditarProjetosParaAlterarEConsultarParaLerAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;
        var soLer = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        var editor = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Editar).BuildAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = soLer;
        (await AmbienteDeTeste.SendAsync(new GetConferenciaDoModeloQuery { VersaoId = versaoId })).Corpos.Should().HaveCount(4);
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().ThrowAsync<ForbiddenAccessException>();
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Unir(projetoId, versaoId, arquivoId, "Aro", "Chaton")))
            .Should().ThrowAsync<ForbiddenAccessException>();

        AmbienteDeTeste.Sessao.PerfilId = editor;
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task DeveRecusarSemMexerNoArquivoQuandoOSolidCraftFalhaAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;
        AmbienteDeTeste.Editor.Falha = new ServicoIndisponivelException("O SolidCraft não respondeu. Tente de novo em instantes.");

        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().ThrowAsync<ServicoIndisponivelException>();

        AmbienteDeTeste.Medidor.Medicao = null;
        await FluentActions.Awaiting(() => AmbienteDeTeste.SendAsync(Material(projetoId, versaoId, arquivoId, "Chaton", Ouro18kAmarelo)))
            .Should().ThrowAsync<ServicoIndisponivelException>().WithMessage("Não foi possível medir o modelo 3D. Tente de novo em instantes.");
        (await HistoricoAsync(projetoId, versaoId)).Should().ContainSingle();
    }

    [Test]
    public async Task DeveConferirPeloGraphQlEAplicarPeloRestAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var arquivoId = await Salvar3dAsync(projetoId, versaoId);
        AmbienteDeTeste.Medidor.Medicao = Anel;

        // O JWT é validado pela hora real.
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        using var http = AmbienteDeTeste.CriarClienteHttp();
        var usuarios = Usar(new UsuarioBuilder()).Admin();
        var login = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(login);
        var token = (await AmbienteDeTeste.SendAsync(new LoginCommand { Email = login.Email, Senha = login.Senha })).AccessToken;

        var resposta = await new ClienteGraphQl(http, token).ExecutarAsync(
            """
            query ($id: Long!, $versaoId: Long!) {
              projeto(id: $id) { versoes(id: $versaoId) { conferenciaDoModelo { arquivoId medido pesoDoMetal corpos { nome uso liga } sobreposicoes { corpoA corpoB podeUnir } } } }
            }
            """,
            new { id = projetoId, versaoId });

        resposta["errors"].Should().BeNull(resposta.ToJsonString());
        var conferencia = resposta["data"]!["projeto"]!["versoes"]![0]!["conferenciaDoModelo"]!;
        conferencia["arquivoId"]!.GetValue<long>().Should().Be(arquivoId);
        conferencia["pesoDoMetal"]!.GetValue<decimal>().Should().Be(5m);
        conferencia["corpos"]!.AsArray().Select(c => (c!["nome"]!.GetValue<string>(), c["uso"]!.GetValue<string>())).Should().Equal(
            ("Aro", "METAL"), ("Chaton", "SEM_MATERIAL"), ("Pedra central", "PEDRA"), ("Guia", "APOIO"));

        using var requisicao = new HttpRequestMessage(HttpMethod.Put, new Uri($"/api/projetos/{projetoId}/versoes/{versaoId}/modelo-3d/materiais", UriKind.Relative))
        {
            Content = JsonContent.Create(new { arquivoId, corpos = new[] { new { corpo = "Chaton", material = Ouro18kAmarelo } } }),
        };
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var put = await http.SendAsync(requisicao);

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var alterado = await put.Content.ReadFromJsonAsync<ModeloAlteradoDto>();
        alterado!.ArquivoAnteriorId.Should().Be(arquivoId);
        (await HistoricoAsync(projetoId, versaoId))[0].Id.Should().Be(alterado.ArquivoId);
    }

    private static DefinirMaterialDosCorposCommand Material(long projetoId, long versaoId, long arquivoId, string corpo, string material) => new()
    {
        ProjetoId = projetoId,
        VersaoId = versaoId,
        ArquivoId = arquivoId,
        Corpos = [new MaterialInformado { Corpo = corpo, Material = material }],
    };

    private static UnirCorposSobrepostosCommand Unir(long projetoId, long versaoId, long arquivoId, params string[] corpos) => new()
    {
        ProjetoId = projetoId,
        VersaoId = versaoId,
        ArquivoId = arquivoId,
        Corpos = corpos,
    };

    private static Task<PrecoDaVersaoDto> PrecoAsync(long versaoId) => AmbienteDeTeste.SendAsync(new GetPrecoDaVersaoQuery { VersaoId = versaoId });

    private static Task<IReadOnlyList<VersaoDoArquivoDto>> HistoricoAsync(long projetoId, long versaoId)
        => AmbienteDeTeste.SendAsync(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Modelo3dNoAssistente.Codigo });

    /// <summary>
    /// O arquivo do Modelo 3D da versão, como a tela salva (o conteúdo não importa: medidor e editor são os de teste).
    /// </summary>
    private static Task<long> Salvar3dAsync(long projetoId, long versaoId)
        => AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Modelo3dNoAssistente.Codigo,
            Conteudo = Convert.ToBase64String(Encoding.UTF8.GetBytes(ConteudoDo3d)),
        });

    /// <summary>
    /// Ouro a R$ 640/g, perda 8%, markup 1,8, mão de obra fixa de R$ 500: com o 3D, o preço depende só do modelo.
    /// </summary>
    private async Task<(long ProjetoId, long VersaoId)> VersaoAsync()
    {
        await AmbienteDeTeste.SendAsync(new AtualizarCotacaoCommand { Metal = Metal.Ouro, Valor = 640m });
        await AmbienteDeTeste.SendAsync(new AtualizarParametrosDePrecoCommand { PerdaPercentual = 8m, ValorHora = 90m, Markup = 1.8m, TaxasPercentual = 0m, ArredondarPara = 10m });
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        await AmbienteDeTeste.SendAsync(new DefinirMaoDeObraCommand { ProjetoId = projetoId, VersaoId = versaoId, Tipo = TipoMaoDeObra.ValorFixo, Valor = 500m });
        return (projetoId, versaoId);
    }
}
