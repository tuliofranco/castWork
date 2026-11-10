using Castwork.Application.Pecas.Commands.AlterarPeca;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Pecas;

/// <summary>
/// Alterar a peça da biblioteca pelo MediatR (a API continua; a tela não usa): só os dados, sem valor.
/// </summary>
public class AlterarPecaCommandTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveAlterarOsDadosDaPecaAsync()
    {
        var id = await Usar(new PecaBuilder()).BuildAsync();

        await AmbienteDeTeste.SendAsync(Alteracao(id));

        var peca = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.AsNoTracking().SingleAsync(p => p.Id == id));
        peca.Should().BeEquivalentTo(new
        {
            Nome = "Fecho lagosta grande",
            Tipo = TipoPeca.Fecho,
            LigaId = (long?)3,
            Material = "Ouro 18k branco",
            Teor = TeorMetal.Ouro18k,
            Peso = Peso.EmGramas(0.5m),
            Descricao = "Com trava dupla",
            AlteradoEm = RelogioDeTeste.Inicio,
        });
    }

    [Test]
    public async Task DeveRecusarPecaInexistenteOuDesativadaAsync()
    {
        var desativada = await Usar(new PecaBuilder()).BuildAsync();
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Pecas.Where(p => p.Id == desativada).ExecuteUpdateAsync(s => s.SetProperty(p => p.Ativo, false)));

        var inexistente = () => AmbienteDeTeste.SendAsync(Alteracao(999_999));
        var inativa = () => AmbienteDeTeste.SendAsync(Alteracao(desativada));

        (await inexistente.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("pecaId");
        (await inativa.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => e.PropertyName).Should().Equal("pecaId");
    }

    private static AlterarPecaCommand Alteracao(long id) => new()
    {
        PecaId = id,
        Nome = "Fecho lagosta grande",
        Tipo = TipoPeca.Fecho,
        LigaId = 3,
        PesoGramas = 0.5m,
        Descricao = "Com trava dupla",
    };
}
