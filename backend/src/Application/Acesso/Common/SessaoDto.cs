namespace Castwork.Application.Acesso.Common;

/// <summary>
/// Resposta de login, renovação e troca de senha: os dois tokens e o resumo do usuário para o front.
/// </summary>
public sealed record SessaoDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiraEm,
    long UsuarioId,
    string Nome,
    bool IsAdmin,
    long? PerfilId);
