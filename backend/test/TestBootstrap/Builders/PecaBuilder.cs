using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Cria a peça com um cliente novo (via <see cref="ClienteBuilder"/>), a menos que <see cref="DoCliente"/> seja usado.
/// </summary>
public sealed class PecaBuilder : DataBuilderAsync<PecaBuilder, CriarPecaCommand>
{
    private static readonly string[] Joias = ["Anel", "Aliança", "Brinco", "Colar", "Pingente", "Pulseira"];
    private static readonly string[] Detalhes = ["solitário", "em ouro 18k", "em prata 925", "com esmeralda", "gota", "personalizado"];

    private readonly ClienteBuilder _clienteBuilder = new();
    private long? _clienteId;

    public PecaBuilder DoCliente(long clienteId)
    {
        _clienteId = clienteId;
        return this;
    }

    protected override async Task<CriarPecaCommand> CriarCommandPadraoAsync()
        => new()
        {
            ClienteId = _clienteId ?? await _clienteBuilder.BuildAsync(),
            Titulo = $"{Faker.PickRandom(Joias)} {Faker.PickRandom(Detalhes)}",
            Descricao = Faker.Lorem.Sentence(),
            PrazoEntrega = DateOnly.FromDateTime(AmbienteDeTeste.Relogio.Agora.UtcDateTime).AddDays(30),
        };

    // Etapas e observações saem em cascata (ON DELETE CASCADE).
    protected override Task RemoverAsync(IReadOnlyCollection<long> ids)
        => AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Pecas.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync());

    protected override Task TearDownCompostosAsync() => _clienteBuilder.TearDownAsync();
}
