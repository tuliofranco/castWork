using Castwork.Application.Projetos.Commands.CriarProjeto;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Projeto sem cliente (projeto autoral), salvo <see cref="DoCliente"/> ou <see cref="ComClienteNovo"/>.
/// Versões e itens saem em cascata com o projeto.
/// </summary>
public sealed class ProjetoBuilder : DataBuilderAsync<ProjetoBuilder, CriarProjetoCommand>
{
    private static readonly string[] Artes = ["Colar", "Pulseira", "Gargantilha", "Tornozeleira", "Brinco"];

    private readonly ClienteBuilder _clienteBuilder = new();
    private long? _clienteId;
    private bool _clienteNovo;

    public ProjetoBuilder DoCliente(long clienteId)
    {
        _clienteId = clienteId;
        return this;
    }

    public ProjetoBuilder ComClienteNovo()
    {
        _clienteNovo = true;
        return this;
    }

    protected override async Task<CriarProjetoCommand> CriarCommandPadraoAsync()
        => new()
        {
            Nome = $"{Faker.PickRandom(Artes)} {Faker.Commerce.ProductAdjective()}",
            Descricao = Faker.Lorem.Sentence(),
            ClienteId = _clienteId ?? (_clienteNovo ? await _clienteBuilder.BuildAsync() : null),
        };

    protected override Task RemoverAsync(IReadOnlyCollection<long> ids)
        => AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Projetos.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync());

    protected override Task TearDownCompostosAsync() => _clienteBuilder.TearDownAsync();
}
