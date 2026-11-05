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
    public async Task DeveCadastrarPecaComTeorEPesoNaBibliotecaAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c =>
        {
            (c.Nome, c.Tipo, c.Material, c.Teor, c.PesoGramas) = ("Elo oval", TipoPeca.Elo, "Ouro amarelo", "18k", 0.35m);
        }).BuildCommandAsync();

        var id = await pecas.SendAsync(command);

        var peca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == id));
        peca.Should().BeEquivalentTo(new
        {
            Nome = "Elo oval",
            Tipo = TipoPeca.Elo,
            Material = "Ouro amarelo",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.35m),
            Ativo = true,
            CriadoEm = RelogioDeTeste.Inicio,
        });
    }

    [Test]
    public async Task DeveRejeitarTeorPesoETipoInvalidosAsync()
    {
        var pecas = Usar(new PecaBuilder());
        var command = await pecas.CustomizeCommand(c => (c.Tipo, c.Teor, c.PesoGramas) = ((TipoPeca)42, "33k", -1m)).BuildCommandAsync();

        var enviar = () => pecas.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName)
            .Should().BeEquivalentTo("tipo", "teor", "pesoGramas");
    }
}
