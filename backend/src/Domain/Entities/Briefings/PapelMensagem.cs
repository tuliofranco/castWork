namespace Castwork.Domain.Entities.Briefings;

public enum PapelMensagem
{
    /// <summary>
    /// O que o joalheiro escreveu no chat.
    /// </summary>
    Joalheiro = 1,

    /// <summary>
    /// Resposta da IA (texto e chamadas de ferramenta).
    /// </summary>
    Assistente = 2,

    /// <summary>
    /// Resultado das ferramentas executadas pelo sistema. Não aparece no chat; só reconstrói a conversa para a IA.
    /// </summary>
    Ferramenta = 3,
}
