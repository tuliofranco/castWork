using Castwork.Application.Abstractions;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Usuário logado controlado pelo teste. Até a Etapa 6 substitui a sessão não autenticada;
/// depois, substitui a sessão lida do JWT.
/// </summary>
public sealed class SessaoDeTeste : ISessaoService
{
    public const long UsuarioPadraoId = 1;

    public long? UsuarioId { get; set; } = UsuarioPadraoId;

    public string? Nome { get; set; } = "Usuário de teste";

    public long? PerfilId { get; set; }

    public bool IsAdmin { get; set; } = true;

    public void Reiniciar()
    {
        UsuarioId = UsuarioPadraoId;
        Nome = "Usuário de teste";
        PerfilId = null;
        IsAdmin = true;
    }
}
