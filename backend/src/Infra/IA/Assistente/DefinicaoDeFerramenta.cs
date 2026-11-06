namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// O que diferencia uma ferramenta com IA das outras: as instruções da IA, o servidor MCP que ela usa
/// e como o arquivo de trabalho entra e sai desse servidor (e vira a prévia SVG da página da versão).
/// O resto da conversa é comum. <see cref="SemMcp"/>: a ferramenta só usa as ferramentas locais da Application
/// (o briefing), sem programa externo nem arquivo. <see cref="Abertura"/>: a fala oculta com que a IA abre a
/// conversa quando o joalheiro ainda não escreveu nada. <see cref="DescreverPasso"/>: como cada ferramenta do MCP
/// aparece no andamento ("Desenhando arcos"); nulo para não mostrar o passo.
/// </summary>
internal sealed record DefinicaoDeFerramenta(
    string Codigo,
    string Instrucoes,
    string? Executavel,
    IReadOnlyList<string> Argumentos,
    string ExtensaoDoArquivo,
    IReadOnlySet<string> FerramentasOcultas,
    IReadOnlySet<string> FerramentasQueAlteram,
    Func<string, (string Nome, string Argumentos)> AbrirArquivo,
    (string Nome, string Argumentos) NovoArquivo,
    Func<string, (string Nome, string Argumentos)> SalvarArquivo,
    Func<string, (string Nome, string Argumentos)>? ExportarPrevia = null,
    bool SemMcp = false,
    string? Abertura = null,
    Func<string, IReadOnlyDictionary<string, System.Text.Json.JsonElement>?, string?>? DescreverPasso = null);
