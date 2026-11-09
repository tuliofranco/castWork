namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// Seção <c>Anthropic</c> da configuração. A chave fica só em User Secrets ou variável de ambiente.
/// </summary>
public sealed class AnthropicOptions
{
    public const string Secao = "Anthropic";

    public string? ApiKey { get; set; }

    /// <summary>
    /// Sonnet por padrão: dá conta das ferramentas e do briefing custando bem menos que o Opus.
    /// </summary>
    public string Modelo { get; set; } = "claude-sonnet-5-5";

    /// <summary>
    /// Limite de idas e vindas com ferramentas num único turno, para a conversa nunca entrar em laço.
    /// </summary>
    public int MaximoDeChamadasPorTurno { get; set; } = 6;

    /// <summary>
    /// Nas ferramentas a IA trabalha mais por turno (desenha, confere, olha o render, corrige).
    /// </summary>
    public int MaximoDeChamadasPorTurnoNasFerramentas { get; set; } = 12;
}
