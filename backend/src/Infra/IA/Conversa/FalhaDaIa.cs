using Anthropic.Exceptions;

namespace Castwork.Infra.IA.Conversa;

/// <summary>
/// Traduz a falha da API em algo que o joalheiro entenda e saiba resolver.
/// </summary>
internal static class FalhaDaIa
{
    public static string Mensagem(AnthropicException ex) => ex.Message switch
    {
        var m when m.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
            => "A conta da IA está sem créditos. Recarregue em console.anthropic.com (Plans & Billing) e tente de novo.",
        var m when m.Contains("authentication", StringComparison.OrdinalIgnoreCase) || m.Contains("x-api-key", StringComparison.OrdinalIgnoreCase)
            => "A chave da IA é inválida. Confira a configuração Anthropic:ApiKey.",
        _ => "A IA não respondeu agora. Tente de novo em instantes.",
    };
}
