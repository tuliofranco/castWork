using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Peça da biblioteca com nome e tipo aleatórios: de metal com uma liga do seed (Ids 1–7), ou pedra com material livre.
/// </summary>
public sealed class PecaBuilder : DataBuilderAsync<PecaBuilder, CriarPecaCommand>
{
    private static readonly (TipoPeca Tipo, string Nome, long? LigaId, string? Material)[] Modelos =
    [
        (TipoPeca.Elo, "Elo oval", 2, null),
        (TipoPeca.Elo, "Elo veneziano", 7, null),
        (TipoPeca.Fecho, "Fecho lagosta", 2, null),
        (TipoPeca.Pedra, "Turmalina", null, "Turmalina"),
        (TipoPeca.Pingente, "Pingente gota", 6, null),
        (TipoPeca.Corrente, "Corrente cartier", 3, null),
    ];

    protected override Task<CriarPecaCommand> CriarCommandPadraoAsync()
    {
        var modelo = Faker.PickRandom(Modelos);
        return Task.FromResult(new CriarPecaCommand
        {
            Nome = $"{modelo.Nome} {Faker.Random.AlphaNumeric(4)}",
            Tipo = modelo.Tipo,
            LigaId = modelo.LigaId,
            Material = modelo.Material,
            PesoGramas = Math.Round(Faker.Random.Decimal(0.1m, 5m), 3),
        });
    }

    /// <summary>
    /// Peça da biblioteca com cópias em projetos não pode ser excluída (FK restrict). Antes, desfaz o vínculo das cópias:
    /// a peça do projeto é dado do builder de projeto, que a remove junto com o projeto.
    /// </summary>
    protected override async Task RemoverAsync(IReadOnlyCollection<long> ids)
    {
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Database.ExecuteSqlAsync(
            $"UPDATE projeto_pecas SET peca_id = NULL WHERE peca_id = ANY({ids.ToArray()})"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Pecas.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync());
    }
}
