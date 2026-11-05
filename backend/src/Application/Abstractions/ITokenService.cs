using Castwork.Domain.Entities.Acesso;

namespace Castwork.Application.Abstractions;

public interface ITokenService
{
    TimeSpan ValidadeDoRefreshToken { get; }

    TokenDeAcesso GerarAccessToken(Usuario usuario, IReadOnlyCollection<string> permissoes, string nomeDoAtelie);

    /// <summary>
    /// Valor opaco e aleatório (256 bits) entregue ao cliente. Nunca é gravado como está.
    /// </summary>
    string GerarRefreshToken();

    /// <summary>
    /// SHA-256 em hexadecimal (64 caracteres): é o que vai para o banco e o que se busca no refresh.
    /// </summary>
    string CalcularHash(string refreshToken);
}
