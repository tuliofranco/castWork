using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

public class CriarPecaCommandTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveCadastrarPecaComLigaEPesoNaBibliotecaAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c =>
        {
            (c.Nome, c.Tipo, c.LigaId, c.Material, c.PesoGramas) = ("Elo oval", TipoPeca.Elo, 2, null, 0.35m);
        }).BuildCommandAsync();

        var id = await pecas.SendAsync(command);

        var peca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == id));
        peca.Should().BeEquivalentTo(new
        {
            Nome = "Elo oval",
            Tipo = TipoPeca.Elo,
            LigaId = (long?)2,
            Material = "Ouro 18k amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.35m),
            Ativo = true,
            CriadoEm = RelogioDeTeste.Inicio,
        });
    }

    [Test]
    public async Task DeveRejeitarLigaInexistentePesoETipoInvalidosAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c => (c.Tipo, c.LigaId, c.Material, c.PesoGramas) = ((TipoPeca)42, 999_999, null, -1m)).BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("tipo", "ligaId", "pesoGramas");
    }
}
