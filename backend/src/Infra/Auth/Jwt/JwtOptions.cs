using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Castwork.Infra.Auth.Jwt;

/// <summary>
/// Configuração dos tokens. <see cref="ChaveAssinatura"/> vem de User Secrets / variável de ambiente
/// (<c>Jwt__ChaveAssinatura</c>), nunca do appsettings versionado.
/// </summary>
public sealed class JwtOptions : IValidatableObject
{
    public const string Secao = "Jwt";
    public const int TamanhoMinimoDaChaveEmBytes = 32;

    [Required]
    public string Emissor { get; init; } = string.Empty;

    [Required]
    public string Audiencia { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt:ChaveAssinatura não configurada (use dotnet user-secrets ou a variável Jwt__ChaveAssinatura).")]
    public string ChaveAssinatura { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutos { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDias { get; init; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // HS256 exige chave de pelo menos 256 bits; chave curta permite ataque de força bruta offline.
        if (Encoding.UTF8.GetByteCount(ChaveAssinatura) < TamanhoMinimoDaChaveEmBytes)
        {
            yield return new ValidationResult(
                $"Jwt:ChaveAssinatura precisa ter pelo menos {TamanhoMinimoDaChaveEmBytes} bytes.",
                [nameof(ChaveAssinatura)]);
        }
    }
}
