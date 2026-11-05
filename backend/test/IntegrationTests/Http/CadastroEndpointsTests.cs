using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Http;

/// <summary>
/// Cadastro pelas telas: POST REST devolve 201 com o Id; a listagem relê pelo GraphQL.
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
        // Dados criados por HTTP não passam por builder: remove aqui, peças antes de clientes (FK).
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.ExecuteDeleteAsync());
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Clientes.ExecuteDeleteAsync());
        _http.Dispose();
    }

    [Test]
    public async Task DeveCadastrarClienteEPecaPorHttpAsync()
    {
        var cliente = await _http.PostAsJsonAsync("/api/clientes", new { nome = "Maria Silva", documento = "529.982.247-25" });
        var clienteId = (await cliente.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var peca = await _http.PostAsJsonAsync("/api/pecas", new { clienteId, titulo = "Aliança", prazoEntrega = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) });

        (cliente.StatusCode, peca.StatusCode).Should().Be((HttpStatusCode.Created, HttpStatusCode.Created));
        var pecaId = (await peca.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var gravada = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == pecaId));
        gravada.Should().BeEquivalentTo(new { ClienteId = clienteId, Titulo = "Aliança", Status = StatusPeca.Briefing }, o => o.ExcludingMissingMembers());
    }

    [Test]
    public async Task DeveResponder400ComErrosPorCampoAoCriarPecaInvalidaAsync()
    {
        var resposta = await _http.PostAsJsonAsync("/api/pecas", new { clienteId = 999_999, titulo = string.Empty });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.Should().BeEquivalentTo("clienteId", "titulo");
    }

    private sealed record IdDto(long Id);
}
