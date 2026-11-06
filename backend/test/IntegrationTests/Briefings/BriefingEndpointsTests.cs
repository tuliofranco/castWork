using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Briefings;

/// <summary>
/// O briefing pela API: formulário, conversa, PDF e download, como o front usa.
/// </summary>
public class BriefingEndpointsTests : TesteDeIntegracao
{
    private HttpClient _http = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        AmbienteDeTeste.Relogio.Agora = DateTimeOffset.UtcNow;
        _http = AmbienteDeTeste.CriarClienteHttp();
        var usuarios = Usar(new UsuarioBuilder()).Admin();
        var command = await usuarios.BuildCommandAsync();
        await usuarios.SendAsync(command);
        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = command.Email, Senha = command.Senha });
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.AccessToken);
    }

    [TearDown]
    public async Task RemoverCriadosPeloHttpAsync()
    {
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.ExecuteDeleteAsync());
        _http.Dispose();
    }

    [Test]
    public async Task DeveMontarOBriefingEBaixarOPdfPorHttpAsync()
    {
        var projetoId = await CriarProjetoAsync();
        var semBriefing = await _http.GetAsync(Rota(projetoId));

        var formulario = await _http.PutAsJsonAsync(Rota(projetoId), new { tipoJoia = "Colar", oQueOClienteQuer = "Colar com pingente de gota", prazo = "2026-12-20" });
        var abertura = await _http.PostAsJsonAsync($"{Rota(projetoId)}/conversa", new { });
        var pdf = await _http.PostAsync(new Uri($"{Rota(projetoId)}/pdf", UriKind.Relative), null);
        var documentoId = (await pdf.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var download = await _http.GetAsync($"{Rota(projetoId)}/documentos/{documentoId}");

        (semBriefing.StatusCode, formulario.StatusCode, abertura.StatusCode, pdf.StatusCode, download.StatusCode)
            .Should().Be((HttpStatusCode.NoContent, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.OK));
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(GeradorDocumentoBriefingDeTeste.Pdf);

        var briefing = await _http.GetFromJsonAsync<BriefingLido>(Rota(projetoId));
        briefing!.TipoJoia.Should().Be("Colar");
        briefing.Mensagens.Should().ContainSingle().Which.Papel.Should().Be("Assistente");
        briefing.Documentos.Should().ContainSingle().Which.Id.Should().Be(documentoId);
    }

    [Test]
    public async Task DevePreVisualizarSemGuardarDocumentoAsync()
    {
        var projetoId = await CriarProjetoAsync();
        await _http.PutAsJsonAsync(Rota(projetoId), new { tipoJoia = "Anel", oQueOClienteQuer = "Anel" });

        var previa = await _http.GetAsync($"{Rota(projetoId)}/pdf/previa");

        previa.StatusCode.Should().Be(HttpStatusCode.OK);
        previa.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await _http.GetFromJsonAsync<BriefingLido>(Rota(projetoId)))!.Documentos.Should().BeEmpty();
    }

    [Test]
    public async Task DeveEnviarImagemNaConversaEServirOArquivoAsync()
    {
        var projetoId = await CriarProjetoAsync();
        await _http.PutAsJsonAsync(Rota(projetoId), new { tipoJoia = "Anel", oQueOClienteQuer = "Anel faixa texturizado" });
        await _http.PostAsJsonAsync($"{Rota(projetoId)}/conversa", new { });
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];

        var resposta = await _http.PostAsJsonAsync($"{Rota(projetoId)}/conversa", new { texto = "Referência", imagens = new[] { Convert.ToBase64String(jpeg) } });
        var briefing = await resposta.Content.ReadFromJsonAsync<BriefingComImagensDto>();
        var chave = briefing!.Mensagens.Single(m => m.Imagens.Count > 0).Imagens.Single();
        var imagem = await _http.GetAsync($"{Rota(projetoId)}/imagens/{chave}");
        var deOutroProjeto = await _http.GetAsync($"{Rota(await CriarProjetoAsync())}/imagens/{chave}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        imagem.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        (await imagem.Content.ReadAsByteArrayAsync()).Should().Equal(jpeg);
        deOutroProjeto.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeveNaoEntregarDocumentoDeOutroProjetoAsync()
    {
        var projetoA = await CriarProjetoAsync();
        var projetoB = await CriarProjetoAsync();
        await _http.PutAsJsonAsync(Rota(projetoA), new { tipoJoia = "Anel", oQueOClienteQuer = "Anel" });
        var documentoDeA = (await (await _http.PostAsync(new Uri($"{Rota(projetoA)}/pdf", UriKind.Relative), null)).Content.ReadFromJsonAsync<IdDto>())!.Id;

        var download = await _http.GetAsync($"{Rota(projetoB)}/documentos/{documentoDeA}");

        download.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeveResponder503QuandoAIaEstaIndisponivelAsync()
    {
        var projetoId = await CriarProjetoAsync();
        await _http.PutAsJsonAsync(Rota(projetoId), new { tipoJoia = "Anel", oQueOClienteQuer = "Anel" });

        var resposta = await _http.PostAsJsonAsync($"{Rota(projetoId)}/conversa", new { texto = "falhe" });

        resposta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task DeveExigirAutenticacaoAsync()
    {
        using var anonimo = AmbienteDeTeste.CriarClienteHttp();

        var resposta = await anonimo.GetAsync(Rota(1));

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string Rota(long projetoId) => $"/api/projetos/{projetoId}/briefing";

    private async Task<long> CriarProjetoAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/projetos", new { nome = "Projeto" });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private sealed record IdDto(long Id);

    private sealed record BriefingLido(string TipoJoia, List<MensagemLida> Mensagens, List<IdDto> Documentos);

    private sealed record MensagemLida(string Papel, string Texto);

    private sealed record BriefingComImagensDto(List<MensagemComImagensDto> Mensagens);

    private sealed record MensagemComImagensDto(List<Guid> Imagens);
}
