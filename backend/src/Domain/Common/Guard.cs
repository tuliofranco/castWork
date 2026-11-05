using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Common;

/// <summary>
/// Validações de invariantes usadas pelas entidades e value objects.
/// A validação amigável (por campo, para o front) fica nos validators da Application;
/// estas são a última linha de defesa do domínio.
/// </summary>
internal static class Guard
{
    public static string Obrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ValorInvalidoException(campo, "é obrigatório");
        }

        return TamanhoMaximo(valor.Trim(), campo, tamanhoMaximo);
    }

    public static string? Opcional(string? valor, string campo, int tamanhoMaximo)
        => string.IsNullOrWhiteSpace(valor) ? null : TamanhoMaximo(valor.Trim(), campo, tamanhoMaximo);

    public static long IdValido(long id, string campo)
        => id > 0 ? id : throw new ValorInvalidoException(campo, "deve ser informado");

    private static string TamanhoMaximo(string valor, string campo, int tamanhoMaximo)
        => valor.Length <= tamanhoMaximo
            ? valor
            : throw new ValorInvalidoException(campo, $"deve ter no máximo {tamanhoMaximo} caracteres");
}
