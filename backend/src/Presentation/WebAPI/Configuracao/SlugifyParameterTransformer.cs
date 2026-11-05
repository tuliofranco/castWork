using System.Text.RegularExpressions;

namespace Castwork.WebAPI.Configuracao;

/// <summary>
/// Rotas em kebab-case: <c>[controller]</c> = AlterarSenha vira <c>alterar-senha</c>.
/// </summary>
public sealed partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
        => value is null ? null : MaiusculaAposMinuscula().Replace(value.ToString()!, "$1-$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex MaiusculaAposMinuscula();
}
