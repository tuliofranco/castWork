using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Exceptions;
using Castwork.Application.Ferramentas.Briefings.Commands.GerarPdfBriefing;
using Castwork.Application.Ferramentas.Briefings.Commands.SalvarFormularioBriefing;
using Castwork.Application.Ferramentas.Briefings.Queries.GetBriefing;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Exceptions;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Ferramentas.Briefings;

/// <summary>
/// Briefing pelo pipeline real, com a IA e o gerador de PDF de teste (determinísticos).
/// </summary>
public class BriefingCommandsTests : TesteDeIntegracao
{
    /// <summary>
    /// Cabeçalho PNG: basta para o tipo ser reconhecido pelos primeiros bytes.
    /// </summary>
    private static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    [Test]
    public async Task DeveCriarOBriefingECorrigirOFormularioNoMesmoRegistroAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();

        var id = await AmbienteDeTeste.SendAsync(Formulario(projetoId));
        var corrigido = await AmbienteDeTeste.SendAsync(Formulario(projetoId, orcamento: "Até R$ 5.000"));

        corrigido.Should().Be(id);
        var briefing = await LerAsync(projetoId);
        briefing.Should().BeEquivalentTo(new { TipoJoia = TipoJoia.Pulseira, Orcamento = "Até R$ 5.000", Prazo = (DateOnly?)new DateOnly(2026, 12, 20) });
    }

    [Test]
    public async Task DeveRecusarFormularioSemOPedidoDoClienteAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var command = Formulario(projetoId);
        command.OQueOClienteQuer = " ";

        var salvar = () => AmbienteDeTeste.SendAsync(command);

        await salvar.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task DeveAbrirAConversaComAMensagemInicialOcultaAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId));

        conversa.Mensagens.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Papel = PapelMensagem.Assistente, Texto = "Olá! Qual a medida?" });
    }

    [Test]
    public async Task DeveRefinarOBriefingPelaConversaDaFerramentaAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId));

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId, " Elos ovais de 4 mm em ouro 18k "));

        (await LerAsync(projetoId)).OQueSeraFeito.Should().Be("Elos ovais de 4 mm em ouro 18k");
        conversa.Mensagens.Select(m => (m.Papel, m.Texto)).Should().Equal(
            (PapelMensagem.Assistente, "Olá! Qual a medida?"),
            (PapelMensagem.Joalheiro, "Elos ovais de 4 mm em ouro 18k"),
            (PapelMensagem.Assistente, "Anotado."));
    }

    [Test]
    public async Task DeveGerarOPdfQuandoAIaPedeAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId));

        await AmbienteDeTeste.SendAsync(Conversar(projetoId, "Pode gerar o PDF"));

        var briefing = await AmbienteDeTeste.SendAsync(new GetBriefingQuery { ProjetoId = projetoId });
        briefing!.Documentos.Should().ContainSingle().Which.NomeArquivo.Should().EndWith(".pdf");
    }

    [Test]
    public async Task DeveSerAMesmaConversaEmQualquerVersaoDoProjetoAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId));

        var pelaVersao = await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, VersaoId = 0, Ferramenta = "briefing" });

        pelaVersao.Mensagens.Should().ContainSingle();
    }

    [Test]
    public async Task DeveGerarPdfComOsDadosDoProjetoEDoClienteAsync()
    {
        var clienteId = await Usar(new ClienteBuilder()).CustomizeCommand(c => c.Nome = "Maria Silva").BuildAsync();
        var projetoId = await Usar(new ProjetoBuilder()).DoCliente(clienteId).CustomizeCommand(c => c.Nome = "Pulseira Raízes").BuildAsync();
        await AmbienteDeTeste.SendAsync(Formulario(projetoId));

        var documentoId = await AmbienteDeTeste.SendAsync(new GerarPdfBriefingCommand { ProjetoId = projetoId });

        documentoId.Should().BePositive();
        var briefingId = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Briefings.Where(b => b.ProjetoId == projetoId).Select(b => b.Id).SingleAsync());
        GeradorDocumentoBriefingDeTeste.UltimosDados.Should().BeEquivalentTo(
            new
            {
                Numero = briefingId,
                Titulo = "Pulseira Raízes",
                Cliente = "Maria Silva",
                OQueSeraFeito = "Pulseira delicada para uso diário",
                Prazo = "20/12/2026",
                Investimento = "Até R$ 3.000",
                Aviso = ConfiguracaoAtelie.AvisoPadrao,
                Marca = new { Nome = "Meu ateliê", Cor = ConfiguracaoAtelie.CorPadrao },
            },
            o => o.ExcludingMissingMembers());
        GeradorDocumentoBriefingDeTeste.UltimosDados!.Especificacoes.Should().StartWith(("Tipo de peça", "Pulseira"));
        var conteudo = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.DocumentosBriefing.Where(d => d.Id == documentoId).Select(d => d.Conteudo).SingleAsync());
        conteudo.Should().Equal(GeradorDocumentoBriefingDeTeste.Pdf);
    }

    [TestCase("4000", "R$ 4.000,00")]
    [TestCase("R$ 3.500,50", "R$ 3.500,50")]
    [TestCase("até R$ 3.000", "até R$ 3.000")]
    public async Task DeveFormatarOInvestimentoQuandoForUmValorAsync(string orcamento, string esperado)
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        await AmbienteDeTeste.SendAsync(Formulario(projetoId, orcamento));

        await AmbienteDeTeste.SendAsync(new GerarPdfBriefingCommand { ProjetoId = projetoId });

        GeradorDocumentoBriefingDeTeste.UltimosDados!.Investimento.Should().Be(esperado);
    }

    [Test]
    public async Task DeveSerSoParaVerDentroDeUmaVersaoAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        var command = Conversar(projetoId, "Oi");
        command.VersaoId = 42;

        var conversar = () => AmbienteDeTeste.SendAsync(command);

        await conversar.Should().ThrowAsync<ValidationException>().WithMessage("*edite pela página do projeto*");
    }

    [Test]
    public async Task DeveExigirOFormularioAntesDaConversaAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId));

        await conversar.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    [Test]
    public async Task DeveExigirTextoDepoisQueAConversaComecouAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId));

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId, "  "));

        await conversar.Should().ThrowAsync<RegraDeNegocioException>();
    }

    [Test]
    public async Task DeveGuardarImagensNaMensagemDoJoalheiroAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId));
        var command = Conversar(projetoId, "Esta é a referência");
        command.Imagens = [Convert.ToBase64String(Png)];

        var conversa = await AmbienteDeTeste.SendAsync(command);

        var chave = conversa.Mensagens.Single(m => m.Papel == PapelMensagem.Joalheiro).Imagens.Should().ContainSingle().Which;
        AssistenteFerramentaDeTeste.UltimoPedido!.Imagens.Should().ContainKey(chave);
    }

    [Test]
    public async Task DeveNaoGravarNadaQuandoAIaFalhaAsync()
    {
        var projetoId = await ProjetoComBriefingAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId, "falhe"));

        await conversar.Should().ThrowAsync<ServicoIndisponivelException>();
        var conversa = await AmbienteDeTeste.SendAsync(new GetConversaFerramentaQuery { ProjetoId = projetoId, Ferramenta = "briefing" });
        conversa.Mensagens.Should().BeEmpty();
    }

    [Test]
    public async Task DeveDevolverNuloSemBriefingAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();

        var briefing = await AmbienteDeTeste.SendAsync(new GetBriefingQuery { ProjetoId = projetoId });

        briefing.Should().BeNull();
    }

    /// <summary>
    /// A conversa do briefing é a da ferramenta "briefing" do projeto (sem versão).
    /// </summary>
    private static ConversarFerramentaCommand Conversar(long projetoId, string? texto = null)
        => new() { ProjetoId = projetoId, Ferramenta = "briefing", Texto = texto };

    private static SalvarFormularioBriefingCommand Formulario(long projetoId, string? orcamento = "Até R$ 3.000") => new()
    {
        ProjetoId = projetoId,
        TipoJoia = TipoJoia.Pulseira,
        OQueOClienteQuer = "Pulseira delicada para uso diário",
        Materiais = "Ouro amarelo",
        Prazo = new DateOnly(2026, 12, 20),
        Orcamento = orcamento,
    };

    private static Task<Briefing> LerAsync(long projetoId)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Briefings.AsNoTracking().SingleAsync(b => b.ProjetoId == projetoId));

    private async Task<long> ProjetoComBriefingAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        await AmbienteDeTeste.SendAsync(Formulario(projetoId));
        return projetoId;
    }
}
