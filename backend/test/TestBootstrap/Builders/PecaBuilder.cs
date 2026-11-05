using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.Domain.Entities.Pecas;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Peça da biblioteca com nome e tipo aleatórios (metal com teor 18k quando for elo, fecho ou corrente).
/// </summary>
public sealed class PecaBuilder : DataBuilderAsync<PecaBuilder, CriarPecaCommand>
{
    private static readonly (TipoPeca Tipo, string Nome, string Material)[] Modelos =
    [
        (TipoPeca.Elo, "Elo oval", "Ouro amarelo"),
        (TipoPeca.Elo, "Elo veneziano", "Prata"),
        (TipoPeca.Fecho, "Fecho lagosta", "Ouro amarelo"),
        (TipoPeca.Pedra, "Turmalina", "Turmalina"),
        (TipoPeca.Pingente, "Pingente gota", "Prata"),
        (TipoPeca.Corrente, "Corrente cartier", "Ouro branco"),
    ];

    protected override Task<CriarPecaCommand> CriarCommandPadraoAsync()
    {
        var modelo = Faker.PickRandom(Modelos);
        var metal = modelo.Tipo is TipoPeca.Elo or TipoPeca.Fecho or TipoPeca.Corrente;
        return Task.FromResult(new CriarPecaCommand
        {
            Nome = $"{modelo.Nome} {Faker.Random.AlphaNumeric(4)}",
            Tipo = modelo.Tipo,
            Material = modelo.Material,
            Teor = metal ? "18k" : null,
            PesoGramas = Math.Round(Faker.Random.Decimal(0.1m, 5m), 3),
        });
    }

    /// <summary>
    /// Peça usada numa composição não pode ser excluída (FK restrict). Antes, desfaz os vínculos dela nas versões:
    /// o item de composição é só a ligação "esta versão usa esta peça", não um dado de outro builder.
    /// </summary>
    protected override async Task RemoverAsync(IReadOnlyCollection<long> ids)
    {
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Database.ExecuteSqlAsync(
            $"DELETE FROM projeto_versao_itens WHERE peca_id = ANY({ids.ToArray()})"));
        await AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Pecas.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync());
    }
}
