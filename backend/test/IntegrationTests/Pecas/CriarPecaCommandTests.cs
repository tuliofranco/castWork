using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

public class CriarPecaCommandTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveCriarPecaEmBriefingComAuditoriaEEtapaInicialAsync()
    {
        var clienteId = await Usar(new ClienteBuilder()).BuildAsync();
        var pecas = Usar(new PecaBuilder()).DoCliente(clienteId);
        var command = await pecas
            .CustomizeCommand(c => (c.Titulo, c.Descricao, c.PrazoEntrega) = ("Aliança", "Ouro 18k", new DateOnly(2026, 12, 10)))
            .BuildCommandAsync();

        var id = await pecas.SendAsync(command);

        var peca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Include(p => p.Etapas).AsNoTracking().SingleAsync(p => p.Id == id));
        peca.Should().BeEquivalentTo(new
        {
            ClienteId = clienteId,
            Titulo = "Aliança",
            Descricao = "Ouro 18k",
            PrazoEntrega = new DateOnly(2026, 12, 10),
            Status = StatusPeca.Briefing,
            CriadoEm = RelogioDeTeste.Inicio,
            CriadoPor = SessaoDeTeste.UsuarioPadraoId,
        });
        peca.Etapas.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Status = StatusPeca.Briefing, IniciadaEm = RelogioDeTeste.Inicio });
    }

    [Test]
    public async Task DeveAceitarPrazoDeHojeNoFusoDeBrasiliaAsync()
    {
        // 02h UTC de 06/11 = 23h de 05/11 em Brasília: "hoje" ainda é 05/11.
        AmbienteDeTeste.Relogio.Agora = new DateTimeOffset(2026, 11, 6, 2, 0, 0, TimeSpan.Zero);
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c => c.PrazoEntrega = new DateOnly(2026, 11, 5)).BuildCommandAsync();

        var id = await pecas.SendAsync(command);

        id.Should().BePositive();
    }

    [Test]
    public async Task DeveRejeitarClienteZeroSemConsultarExistenciaAsync()
    {
        var pecas = Usar(new PecaBuilder()).DoCliente(0);
        var command = await pecas.BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "clienteId", ErrorMessage = "Informe o cliente." });
    }

    [Test]
    public async Task DeveRejeitarClienteInexistenteAsync()
    {
        var pecas = Usar(new PecaBuilder()).DoCliente(999_999);
        var command = await pecas.BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "clienteId", ErrorMessage = "Cliente não encontrado." });
    }

    [Test]
    public async Task DeveRejeitarClienteInativoAsync()
    {
        var clienteId = await Usar(new ClienteBuilder()).BuildAsync();
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Clientes.Where(c => c.Id == clienteId).ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativo, false)));
        var pecas = Usar(new PecaBuilder()).DoCliente(clienteId);
        var command = await pecas.BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(new { PropertyName = "clienteId", ErrorMessage = "Cliente não encontrado." });
    }

    [Test]
    public async Task DeveRejeitarTituloVazioEPrazoNoPassadoAsync()
    {
        var pecas = Usar(new PecaBuilder())
            .CustomizeCommand(c => (c.Titulo, c.PrazoEntrega) = (" ", new DateOnly(2026, 11, 4)))
            ;
        var command = await pecas.BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => (e.PropertyName, e.ErrorMessage)).Should().BeEquivalentTo(new[]
        {
            ("titulo", "'Título' deve ser informado."),
            ("prazoEntrega", "O prazo de entrega não pode estar no passado."),
        });
    }
}
