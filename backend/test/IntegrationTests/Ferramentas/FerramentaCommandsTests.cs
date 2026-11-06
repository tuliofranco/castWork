using System.Text;

using Castwork.Application.Common.Exceptions;
using Castwork.Application.Ferramentas.Commands;
using Castwork.Application.Ferramentas.Queries;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Ferramentas;

/// <summary>
/// Conversa e arquivo das ferramentas com IA pelo pipeline real, com a IA de teste (determinística).
/// </summary>
public class FerramentaCommandsTests : TesteDeIntegracao
{
    private const string Desenho = "desenho-2d";

    [Test]
    public async Task DeveConversarSemMudarOArquivoQuandoAIaSoRespondeAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Qual a espessura ideal?"));

        conversa.Mensagens.Select(m => (m.Papel, m.Texto)).Should().Equal((PapelMensagem.Joalheiro, "Qual a espessura ideal?"), (PapelMensagem.Assistente, "Anotado."));
        conversa.ArquivoId.Should().BeNull();
        conversa.Mensagens.Should().OnlyContain(m => m.Acao == null);
    }

    [Test]
    public async Task DeveGravarOArquivoNovoComOCartaoDaAcaoEORenderAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe as 3 vistas"));

        var resposta = conversa.Mensagens[^1];
        resposta.Acao.Should().BeEquivalentTo(new AcaoDaIaDto("Desenhei a peça", false));
        var render = resposta.Imagens.Should().ContainSingle().Which;
        conversa.ArquivoId.Should().NotBeNull();

        var arquivo = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        Encoding.UTF8.GetString(arquivo!.Conteudo).Should().Be("DXF:Desenhe as 3 vistas");
        var imagem = await AmbienteDeTeste.SendAsync(new GetImagemConversaQuery { ProjetoId = projetoId, VersaoId = versaoId, Chave = render });
        imagem.Conteudo.Should().Equal(AssistenteFerramentaDeTeste.Render);
    }

    [Test]
    public async Task DeveMandarParaAIaOArquivoAtualEOContextoDaVersaoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o anel"));

        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "E agora?"));

        var pedido = AssistenteFerramentaDeTeste.UltimoPedido!;
        Encoding.UTF8.GetString(pedido.Arquivo!).Should().Be("DXF:Desenhe o anel");
        pedido.Contexto.Should().Contain("\"nome\":\"Base\"");
        pedido.Historico.Should().HaveCount(2);
    }

    [Test]
    public async Task DeveDesfazerAUltimaAcaoVoltandoOArquivoDeAntesAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe a frente"));
        var segunda = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe a lateral"));

        var desfeita = await AmbienteDeTeste.SendAsync(new DesfazerAcaoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, Ordem = segunda.Mensagens[^1].Ordem });

        desfeita.Mensagens[^1].Acao!.Desfeita.Should().BeTrue();
        var arquivo = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        Encoding.UTF8.GetString(arquivo!.Conteudo).Should().Be("DXF:Desenhe a frente");
        arquivo.Id.Should().Be(desfeita.ArquivoId!.Value);
    }

    [Test]
    public async Task DeveDesfazerAPrimeiraAcaoSemApagarOQueOJoalheiroSalvouAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var primeira = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o anel"));
        var salvo = await SalvarAsync(projetoId, versaoId, "DXF:ajustado");

        var desfeita = await AmbienteDeTeste.SendAsync(new DesfazerAcaoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, Ordem = primeira.Mensagens[^1].Ordem });

        desfeita.ArquivoId.Should().BeNull("voltou ao desenho em branco");
        (await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho })).Should().BeNull();
        var historico = await HistoricoAsync(projetoId, versaoId);
        historico.Select(v => (v.Origem, v.Tamanho == 0)).Should().Equal((OrigemArquivo.Desfazer, true), (OrigemArquivo.Joalheiro, false), (OrigemArquivo.Ia, false));
        var versao = await AmbienteDeTeste.SendAsync(new GetVersaoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = salvo });
        Encoding.UTF8.GetString(versao!.Conteudo).Should().Be("DXF:ajustado");
    }

    [Test]
    public async Task DeveRestaurarUmaVersaoDoHistoricoComoAtualAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var salvo = await SalvarAsync(projetoId, versaoId, "DXF:meu");
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe por cima"));

        var restaurado = await AmbienteDeTeste.SendAsync(new RestaurarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = salvo });

        var arquivo = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        arquivo!.Id.Should().Be(restaurado!.Value);
        Encoding.UTF8.GetString(arquivo.Conteudo).Should().Be("DXF:meu");
        (await HistoricoAsync(projetoId, versaoId)).Select(v => v.Origem).Should().Equal(OrigemArquivo.Restauracao, OrigemArquivo.Ia, OrigemArquivo.Joalheiro);
    }

    [Test]
    public async Task DeveRecusarRestaurarArquivoDeOutraVersaoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var outraVersao = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Outra" });
        var daOutra = await SalvarAsync(projetoId, outraVersao, "DXF:outra");
        await SalvarAsync(projetoId, versaoId, "DXF:esta");

        var restaurar = () => AmbienteDeTeste.SendAsync(new RestaurarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = daOutra });

        await restaurar.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    [Test]
    public async Task DeveGerarAPreviaDoArquivoAtualSoQuandoHaDesenhoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var consulta = new GetPreviaArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho };
        (await AmbienteDeTeste.SendAsync(consulta)).Should().BeNull("ainda não há desenho");

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o aro"));

        var previa = await AmbienteDeTeste.SendAsync(consulta);
        previa!.ArquivoId.Should().Be(conversa.ArquivoId!.Value);
        Encoding.UTF8.GetString(previa.Svg).Should().Contain("DXF:Desenhe o aro");

        var salvo = await SalvarAsync(projetoId, versaoId, "DXF:depois");
        consulta.ArquivoId = previa.ArquivoId;
        Encoding.UTF8.GetString((await AmbienteDeTeste.SendAsync(consulta))!.Svg).Should().Contain("DXF:Desenhe o aro", "a prévia é do arquivo pedido, não do atual");
        salvo.Should().NotBe(previa.ArquivoId);
    }

    [Test]
    public async Task DeveListarOsArquivosDaVersaoPaginadosSemODesenhoEmBrancoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var primeira = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o anel"));
        await AmbienteDeTeste.SendAsync(new DesfazerAcaoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, Ordem = primeira.Mensagens[^1].Ordem });
        for (var i = 0; i < 11; i++)
        {
            await SalvarAsync(projetoId, versaoId, $"DXF:{i}");
        }

        var outraVersao = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Outra" });
        await SalvarAsync(projetoId, outraVersao, "DXF:outra");

        var pagina1 = await AmbienteDeTeste.SendAsync(new GetArquivosDaVersaoQuery { ProjetoId = projetoId, VersaoId = versaoId, TamanhoPagina = 10 });
        var pagina2 = await AmbienteDeTeste.SendAsync(new GetArquivosDaVersaoQuery { ProjetoId = projetoId, VersaoId = versaoId, TamanhoPagina = 10, Pagina = 2 });

        pagina1.TotalRegistros.Should().Be(12, "11 salvos + o da IA; o em branco fica de fora");
        pagina1.Lista.Should().HaveCount(10).And.OnlyContain(a => a.Ferramenta == Desenho);
        pagina1.Lista.Select(a => a.Id).Should().BeInDescendingOrder();
        pagina2.Lista.Should().HaveCount(2);
        pagina2.Lista[^1].Origem.Should().Be(OrigemArquivo.Ia);
    }

    [Test]
    public async Task DeveArquivarOAtualVoltandoOAnteriorEApagarSoArquivadoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var antigo = await SalvarAsync(projetoId, versaoId, "DXF:antigo");
        var atual = await SalvarAsync(projetoId, versaoId, "DXF:atual");

        var apagarSemArquivar = () => AmbienteDeTeste.SendAsync(new ApagarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = atual });
        await apagarSemArquivar.Should().ThrowAsync<RegraDeNegocioException>().WithMessage("*Arquive*");

        await AmbienteDeTeste.SendAsync(new RenomearArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = antigo, Nome = "  Proposta A  " });
        await AmbienteDeTeste.SendAsync(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = atual });

        var arquivo = await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        arquivo!.Id.Should().Be(antigo, "o atual arquivado deixa o anterior como atual");
        var ativos = await AmbienteDeTeste.SendAsync(new GetArquivosDaVersaoQuery { ProjetoId = projetoId, VersaoId = versaoId });
        ativos.Lista.Should().ContainSingle().Which.Nome.Should().Be("Proposta A");
        var arquivados = await AmbienteDeTeste.SendAsync(new GetArquivosDaVersaoQuery { ProjetoId = projetoId, VersaoId = versaoId, Arquivados = true });
        arquivados.Lista.Should().ContainSingle().Which.Id.Should().Be(atual);

        await AmbienteDeTeste.SendAsync(new ApagarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = atual });

        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ArquivosFerramenta.AnyAsync(a => a.Id == atual))).Should().BeFalse();
        await AmbienteDeTeste.SendAsync(new ArquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = antigo });
        (await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho })).Should().BeNull("tudo arquivado: o editor abre em branco");
        await AmbienteDeTeste.SendAsync(new DesarquivarArquivoFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, ArquivoId = antigo });
        (await AmbienteDeTeste.SendAsync(new GetArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho }))!.Id.Should().Be(antigo);
    }

    [Test]
    public async Task DeveSalvarOArquivoEditadoNaTelaComoAtualAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var id = await AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Desenho,
            Conteudo = Convert.ToBase64String("DXF:mão"u8.ToArray()),
        });
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Confere"));

        AssistenteFerramentaDeTeste.UltimoPedido!.Arquivo.Should().Equal("DXF:mão"u8.ToArray());
        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ArquivosFerramenta.SingleAsync(a => a.Id == id))).Origem.Should().Be(OrigemArquivo.Joalheiro);
    }

    [Test]
    public async Task DeveApagarAConversaMantendoOArquivoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe"));

        await AmbienteDeTeste.SendAsync(new ApagarConversaFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });

        var conversa = await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        conversa.Mensagens.Should().BeEmpty();
        conversa.ArquivoId.Should().NotBeNull();
        (await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ImagensConversa.CountAsync())).Should().Be(0);
    }

    [Test]
    public async Task DeveManterUmaConversaPorFerramentaEmCadaVersaoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var outraVersao = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Outra" });
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Na primeira"));

        var naOutra = await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = outraVersao, Ferramenta = Desenho });

        naOutra.Mensagens.Should().BeEmpty();
    }

    [Test]
    public async Task DeveRecusarFerramentaDesconhecidaAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        var command = Conversar(projetoId, versaoId, "Oi");
        command.Ferramenta = "modelagem-3d";

        var conversar = () => AmbienteDeTeste.SendAsync(command);

        await conversar.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task DeveRecusarVersaoDeOutroProjetoAsync()
    {
        var (_, versaoId) = await VersaoAsync();
        var (outroProjeto, _) = await VersaoAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(outroProjeto, versaoId, "Oi"));

        await conversar.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    [Test]
    public async Task DeveNaoGravarMensagensQuandoAIaFalhaAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "falhe"));

        await conversar.Should().ThrowAsync<ServicoIndisponivelException>();
        var conversa = await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });
        conversa.Mensagens.Should().BeEmpty();
    }

    private static Task<long> SalvarAsync(long projetoId, long versaoId, string conteudo)
        => AmbienteDeTeste.SendAsync(new SalvarArquivoFerramentaCommand
        {
            ProjetoId = projetoId,
            VersaoId = versaoId,
            Ferramenta = Desenho,
            Conteudo = Convert.ToBase64String(Encoding.UTF8.GetBytes(conteudo)),
        });

    private static Task<IReadOnlyList<VersaoDoArquivoDto>> HistoricoAsync(long projetoId, long versaoId)
        => AmbienteDeTeste.SendAsync(new GetHistoricoArquivoFerramentaQuery { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho });

    private static ConversarFerramentaCommand Conversar(long projetoId, long versaoId, string texto)
        => new() { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, Texto = texto };

    private async Task<(long ProjetoId, long VersaoId)> VersaoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        return (projetoId, versaoId);
    }
}
