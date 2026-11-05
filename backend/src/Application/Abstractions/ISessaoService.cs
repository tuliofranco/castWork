namespace Castwork.Application.Abstractions;

/// <summary>
/// Usuário da requisição atual, lido exclusivamente das claims do token validado.
/// Fora de uma requisição (jobs, migrations) não há usuário: <see cref="UsuarioId"/> é <c>null</c>.
/// </summary>
public interface ISessaoService
{
    long? UsuarioId { get; }

    string? Nome { get; }

    long? PerfilId { get; }

    bool IsAdmin { get; }

    bool EstaAutenticado => UsuarioId is not null;
}
