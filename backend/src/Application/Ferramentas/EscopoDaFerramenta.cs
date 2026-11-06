namespace Castwork.Application.Ferramentas;

/// <summary>
/// Onde vive a conversa de cada ferramenta: na versão (o desenho de cada versão) ou no projeto (o briefing,
/// que vale para todas as versões).
/// </summary>
public static class EscopoDaFerramenta
{
    public const string Briefing = "briefing";

    public static bool DoProjeto(string ferramenta) => ferramenta == Briefing;

    /// <summary>
    /// A versão que identifica a conversa: nula nas ferramentas do projeto, qualquer que seja a rota.
    /// </summary>
    public static long? Versao(string ferramenta, long versaoId) => DoProjeto(ferramenta) ? null : versaoId;
}
