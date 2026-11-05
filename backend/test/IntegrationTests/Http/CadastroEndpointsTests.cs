using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Domain.Entities.Projetos;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Http;

/// <summary>
/// Fluxo das telas por HTTP: cliente → peça → projeto → composição → publicação.
/// </summary>
public class CadastroEndpointsTests : TesteDeIntegracao
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
        // Dados criados por HTTP não passam por builder: remove aqui respeitando as FKs.
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.ExecuteDeleteAsync());
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.ExecuteDeleteAsync());
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Clientes.ExecuteDeleteAsync());
        _http.Dispose();
    }

    [Test]
    public async Task DeveMontarEPublicarUmProjetoPorHttpAsync()
    {
        var clienteId = await CriarAsync("/api/clientes", new { nome = "Maria Silva" });
        var eloId = await CriarAsync("/api/pecas", new { nome = "Elo oval", tipo = "Elo", material = "Ouro amarelo", teor = "18k", pesoGramas = 0.35m });
        var projetoId = await CriarAsync("/api/projetos", new { nome = "Colar Raízes", clienteId });
        var v1 = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == projetoId).SelectMany(p => p.Versoes).Select(v => v.Id).SingleAsync());

        var item = await _http.PostAsJsonAsync($"/api/projetos/{projetoId}/versoes/{v1}/itens", new { pecaId = eloId, quantidade = 12 });
        var publicar = await _http.PostAsync(new Uri($"/api/projetos/{projetoId}/versoes/{v1}/publicar", UriKind.Relative), null);
        var depoisDePublicada = await _http.PostAsJsonAsync($"/api/projetos/{projetoId}/versoes/{v1}/itens", new { pecaId = eloId, quantidade = 1 });

        (item.StatusCode, publicar.StatusCode, depoisDePublicada.StatusCode)
            .Should().Be((HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.UnprocessableEntity));
        (await depoisDePublicada.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Contain("publicada");
        var versao = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Projetos.Where(p => p.Id == projetoId)
            .SelectMany(p => p.Versoes).Include(v => v.Itens).AsNoTracking().SingleAsync());
        versao.Should().BeEquivalentTo(new { Status = StatusVersao.Publicada }, o => o.ExcludingMissingMembers());
        versao.Itens.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PecaId = eloId, Quantidade = 12 });
    }

    [Test]
    public async Task DeveResponder404ParaVersaoInexistenteE400ParaProjetoInvalidoAsync()
    {
        var projetoId = await CriarAsync("/api/projetos", new { nome = "Pulseira" });
        var eloId = await CriarAsync("/api/pecas", new { nome = "Elo", tipo = "Elo" });

        var versaoInexistente = await _http.PostAsJsonAsync($"/api/projetos/{projetoId}/versoes/999999/itens", new { pecaId = eloId, quantidade = 1 });
        var projetoInvalido = await _http.PostAsJsonAsync("/api/projetos", new { nome = string.Empty, clienteId = 999_999 });

        versaoInexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);
        projetoInvalido.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await projetoInvalido.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().BeEquivalentTo("nome", "clienteId");
    }

    private async Task<long> CriarAsync(string rota, object corpo)
    {
        var resposta = await _http.PostAsJsonAsync(rota, corpo);
        resposta.StatusCode.Should().Be(HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        return (await resposta.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private sealed record IdDto(long Id);
}
