using Castwork.Application.Acesso.Commands.CriarPerfil;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

public sealed class PerfilBuilder : DataBuilderAsync<PerfilBuilder, CriarPerfilCommand>
{
    /// <summary>
    /// Regras do catálogo (constantes de <c>Regras</c>).
    /// </summary>
    public PerfilBuilder ComRegras(params string[] regras)
        => CustomizeCommand(c => c.Regras = [.. regras.Select(Guid.Parse)]);

    protected override Task<CriarPerfilCommand> CriarCommandPadraoAsync()
        => Task.FromResult(new CriarPerfilCommand { Nome = $"Perfil {Faker.Random.AlphaNumeric(8)}" });

    protected override Task RemoverAsync(IReadOnlyCollection<long> ids)
        => AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Perfis.Where(p => ids.Contains(p.Id)).ExecuteDeleteAsync());
}
