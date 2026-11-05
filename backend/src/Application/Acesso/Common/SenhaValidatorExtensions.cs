using FluentValidation;

namespace Castwork.Application.Acesso.Common;

public static class SenhaValidatorExtensions
{
    public const int TamanhoMinimo = 8;
    public const int TamanhoMaximo = 128;

    /// <summary>
    /// Política de senha alinhada ao NIST SP 800-63B: comprimento mínimo e máximo, sem exigir
    /// combinações artificiais de caracteres (que levam a senhas previsíveis como "Senha@123").
    /// </summary>
    public static IRuleBuilderOptions<T, string> SenhaForte<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty()
            .MinimumLength(TamanhoMinimo)
            .MaximumLength(TamanhoMaximo)
            .Must(senha => senha.Trim().Length == senha.Length).WithMessage("A senha não pode começar nem terminar com espaço.");
}
