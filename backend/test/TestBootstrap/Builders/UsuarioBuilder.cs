using Bogus;

using Castwork.Application.Acesso.Commands.CriarUsuario;
using Castwork.TestBootstrap.Ambiente;

using Microsoft.EntityFrameworkCore;

namespace Castwork.TestBootstrap.Builders;

/// <summary>
/// Usuário não admin com um perfil novo (via <see cref="PerfilBuilder"/>), salvo <see cref="Admin"/> ou <see cref="DoPerfil"/>.
/// Todos nascem com a <see cref="SenhaPadrao"/>.
/// </summary>
public sealed class UsuarioBuilder : DataBuilderAsync<UsuarioBuilder, CriarUsuarioCommand>
{
    public const string SenhaPadrao = "senha-de-teste-123";

    private readonly PerfilBuilder _perfilBuilder = new();
    private long? _perfilId;
    private bool _admin;

    public UsuarioBuilder Admin()
    {
        _admin = true;
        return this;
    }

    public UsuarioBuilder DoPerfil(long perfilId)
    {
        _perfilId = perfilId;
        return this;
    }

    protected override async Task<CriarUsuarioCommand> CriarCommandPadraoAsync()
    {
        var pessoa = new Person("pt_BR");
        return new CriarUsuarioCommand
        {
            Nome = pessoa.FullName,
            Email = $"{Faker.Random.AlphaNumeric(10)}@castwork.test",
            Senha = SenhaPadrao,
            IsAdmin = _admin,
            PerfilId = _admin ? _perfilId : _perfilId ?? await _perfilBuilder.BuildAsync(),
        };
    }

    // Refresh tokens saem em cascata.
    protected override Task RemoverAsync(IReadOnlyCollection<long> ids)
        => AmbienteDeTeste.ExecutarNoBancoAsync(context => context.Usuarios.Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync());

    protected override Task TearDownCompostosAsync() => _perfilBuilder.TearDownAsync();
}
