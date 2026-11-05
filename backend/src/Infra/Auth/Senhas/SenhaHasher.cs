using Castwork.Application.Abstractions;

using Microsoft.AspNetCore.Identity;

namespace Castwork.Infra.Auth.Senhas;

/// <summary>
/// Hash de senha do ASP.NET Core Identity: PBKDF2 com HMAC-SHA512, salt aleatório e 100 mil iterações.
/// O formato guarda os parâmetros, então quando o padrão ficar mais forte o login avisa que precisa de rehash.
/// </summary>
public sealed class SenhaHasher : ISenhaHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Usuario = new();
    private static readonly Lazy<string> HashFicticio = new(() => Hasher.HashPassword(Usuario, Guid.NewGuid().ToString()));

    public string Hash(string senha) => Hasher.HashPassword(Usuario, senha);

    public ResultadoVerificacaoSenha Verificar(string hash, string senha)
        => Hasher.VerifyHashedPassword(Usuario, hash, senha) switch
        {
            PasswordVerificationResult.Success => ResultadoVerificacaoSenha.Valida,
            PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacaoSenha.ValidaPrecisaRehash,
            _ => ResultadoVerificacaoSenha.Invalida,
        };

    public void SimularVerificacao(string senha) => Hasher.VerifyHashedPassword(Usuario, HashFicticio.Value, senha);
}
