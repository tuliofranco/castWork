namespace Castwork.Infra.IA.Ferramentas;

/// <summary>
/// O que diferencia uma ferramenta com IA das outras: as instruções da IA, o servidor MCP que ela usa
/// e como o arquivo de trabalho entra e sai desse servidor (e vira a prévia SVG da página da versão).
/// O resto da conversa é comum.
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
    Func<string, (string Nome, string Argumentos)>? ExportarPrevia = null);
