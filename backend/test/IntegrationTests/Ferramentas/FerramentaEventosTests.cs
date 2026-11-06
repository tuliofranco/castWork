using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Ferramentas.Commands;
using Castwork.Application.Projetos.Commands.CriarVersao;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Ferramentas;

/// <summary>
/// O acompanhamento ao vivo do turno pela API (Server-Sent Events), como a tela usa.
/// </summary>
public class FerramentaEventosTests : TesteDeIntegracao
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
    public async Task DeveEntregarOQueJaAconteceuEOQueVemAteOFimAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();
        await AmbienteDeTeste.SendAsync(new ConversarFerramentaCommand { ProjetoId = projetoId, VersaoId = versaoId, Ferramenta = "desenho-2d", Texto = "Oi" });
        var conversaId = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.ConversasFerramenta.Select(c => c.Id).SingleAsync());
        var painel = AmbienteDeTeste.Obter<IPainelDoTurno>();
        painel.Comecar(conversaId);
        painel.Publicar(conversaId, new EventoDoTurno("pensamento", "Vou desenhar"));

        var leitura = LerEventosAsync(projetoId, versaoId);
        await Task.Delay(300);
        painel.Publicar(conversaId, new EventoDoTurno("passo", "Desenhando arcos", 1, "andamento"));
        painel.Terminar(conversaId);

        (await leitura).Select(e => e.GetProperty("tipo").GetString()).Should().Equal("pensamento", "passo", "fim");
    }

    [Test]
    public async Task DeveTerminarNaHoraQuandoNaoHaTurnoAsync()
    {
        var (projetoId, versaoId) = await VersaoAsync();

        var eventos = await LerEventosAsync(projetoId, versaoId).WaitAsync(TimeSpan.FromSeconds(5));

        eventos.Select(e => e.GetProperty("tipo").GetString()).Should().Equal("fim");
    }

    private async Task<List<JsonElement>> LerEventosAsync(long projetoId, long versaoId)
    {
        using var resposta = await _http.GetAsync($"/api/projetos/{projetoId}/versoes/{versaoId}/ferramentas/desenho-2d/conversa/eventos", HttpCompletionOption.ResponseHeadersRead);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        var eventos = new List<JsonElement>();
        using var leitor = new StreamReader(await resposta.Content.ReadAsStreamAsync());
        while (await leitor.ReadLineAsync() is { } linha)
        {
            if (linha.StartsWith("data: ", StringComparison.Ordinal))
            {
                eventos.Add(JsonDocument.Parse(linha[6..]).RootElement.Clone());
            }
        }

        return eventos;
    }

    private async Task<(long ProjetoId, long VersaoId)> VersaoAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/projetos", new { nome = "Projeto" });
        var projetoId = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        var versaoId = await AmbienteDeTeste.SendAsync(new CriarVersaoCommand { ProjetoId = projetoId, Nome = "Base" });
        return (projetoId, versaoId);
    }
}
