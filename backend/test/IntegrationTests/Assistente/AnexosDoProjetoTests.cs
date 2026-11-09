using System.Text;

using Castwork.Application.Assistente.Commands;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Ferramentas.Briefings.Commands.GerarPdfBriefing;
using Castwork.Application.Ferramentas.Briefings.Commands.SalvarFormularioBriefing;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Exceptions;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

namespace Castwork.IntegrationTests.Assistente;

/// <summary>
/// Anexos "Do projeto" na conversa: o que pode ser anexado (arquivos das ferramentas e o PDF do briefing),
/// o que vai para a IA e o que a mensagem guarda.
/// </summary>
public class AnexosDoProjetoTests : TesteDeIntegracao
{
    private const string Desenho = "desenho-2d";

    [Test]
    public async Task DeveListarOArquivoAtualDeCadaFerramentaEOUltimoPdfDoBriefingAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o anel"));
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe de novo"));
        await AmbienteDeTeste.SendAsync(Formulario(projetoId));
        await AmbienteDeTeste.SendAsync(new GerarPdfBriefingCommand { ProjetoId = projetoId });
        var pdfId = await AmbienteDeTeste.SendAsync(new GerarPdfBriefingCommand { ProjetoId = projetoId });

        var anexos = await AmbienteDeTeste.SendAsync(new GetAnexosDoProjetoQuery { ProjetoId = projetoId });

        anexos.Select(a => (a.Origem, a.Ferramenta, a.Versao)).Should().BeEquivalentTo([("arquivo", Desenho, (int?)1), ("briefing", "briefing", (int?)null)]);
        anexos.Single(a => a.Origem == "briefing").Id.Should().Be(pdfId, "só o último PDF");
    }

    [Test]
    public async Task DeveMandarOArquivoAnexadoAIaEGuardarSoAReferenciaNaFalaAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Desenhe o anel"));
        var arquivo = (await AmbienteDeTeste.SendAsync(new GetAnexosDoProjetoQuery { ProjetoId = projetoId })).Single();

        var conversa = await AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Use este", new AnexoPedido("arquivo", arquivo.Id)));

        var anexo = AssistenteFerramentaDeTeste.UltimoPedido!.Anexos!.Should().ContainSingle().Subject;
        anexo.Ferramenta.Should().Be(Desenho);
        anexo.Descricao.Should().Be("desenho-2d · versão 1");
        Encoding.UTF8.GetString(anexo.Conteudo).Should().Be("DXF:Desenhe o anel");
        conversa.Mensagens[^2].Anexos.Should().BeEquivalentTo([new AnexoDaMensagemDto("arquivo", arquivo.Id, arquivo.Nome)]);
    }

    [Test]
    public async Task DeveRecusarArquivoDeOutroProjetoAsync()
    {
        var (outroProjeto, outraVersao) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(Conversar(outroProjeto, outraVersao, "Desenhe o anel"));
        var deOutro = (await AmbienteDeTeste.SendAsync(new GetAnexosDoProjetoQuery { ProjetoId = outroProjeto })).Single();
        var (projetoId, versaoId) = await VersaoAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Use este", new AnexoPedido("arquivo", deOutro.Id)));

        await conversar.Should().ThrowAsync<EntidadeNaoEncontradaException>();
    }

    [Test]
    public async Task DeveLimitarATresArquivosPorMensagemAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var conversar = () => AmbienteDeTeste.SendAsync(Conversar(projetoId, versaoId, "Use", [.. Enumerable.Range(1, 4).Select(i => new AnexoPedido("arquivo", i))]));

        await conversar.Should().ThrowAsync<ValidationException>();
    }

    private static ConversarFerramentaCommand Conversar(long projetoId, long versaoId, string texto, params AnexoPedido[] anexos)
        => new() { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = Desenho, Texto = texto, Anexos = anexos };

    private static SalvarFormularioBriefingCommand Formulario(long projetoId) => new()
    {
        ProjetoId = projetoId,
        TipoJoia = TipoJoia.Pulseira,
        OQueOClienteQuer = "Pulseira delicada para uso diário",
        Materiais = "Ouro amarelo",
        Prazo = new DateOnly(2026, 12, 20),
    };

    private async Task<(long ProjetoId, long VersaoId)> VersaoAsync()
    {
        var projetoId = await Usar(new ProjetoBuilder()).BuildAsync();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        return (projetoId, versaoId);
    }
}
