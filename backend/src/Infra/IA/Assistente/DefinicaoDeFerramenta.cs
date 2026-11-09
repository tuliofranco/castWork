namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// O que diferencia uma ferramenta com IA das outras: as instruções da IA, o servidor MCP que ela usa
/// e como o arquivo de trabalho entra e sai desse servidor (e vira a prévia SVG da página da versão).
/// O resto da conversa é comum. <see cref="SemMcp"/>: a ferramenta só usa as ferramentas locais da Application
/// (o briefing), sem programa externo nem arquivo. <see cref="Abertura"/>: a fala oculta com que a IA abre a
/// conversa quando o joalheiro ainda não escreveu nada. <see cref="DescreverPasso"/>: como cada ferramenta do MCP
/// aparece no andamento ("Desenhando arcos"); nulo para não mostrar o passo. <see cref="AjustarEntrada"/>: muda os
/// argumentos que a IA mandou antes de chamar o MCP (ex.: limitar o tamanho do screenshot).
/// <see cref="CamposInternos"/>: campos dos resultados do MCP que não servem à IA (saem antes de ir para ela).
/// <see cref="ResumoDoArquivo"/>: depois de cada rodada que alterou o arquivo, o sistema faz esta chamada e anexa o
/// resumo ao resultado (a IA confere sem pedir imagem). <see cref="VistaParaOJoalheiro"/>: no fim de um turno que
/// alterou o arquivo, a imagem que aparece com a resposta; só o joalheiro a vê, ela não vai para a IA.
/// <see cref="ModeloPadrao"/>: o modelo quando o joalheiro não escolheu (<c>haiku</c>, <c>sonnet</c>, <c>opus</c>);
/// nulo usa o da configuração.
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
    Func<string, IReadOnlyDictionary<string, System.Text.Json.JsonElement>?, string?>? DescreverPasso = null,
    Func<string, Dictionary<string, System.Text.Json.JsonElement>, Dictionary<string, System.Text.Json.JsonElement>>? AjustarEntrada = null,
    IReadOnlySet<string>? CamposInternos = null,
    ((string Nome, string Argumentos) Chamada, Func<string, string?> Resumir)? ResumoDoArquivo = null,
    (string Nome, string Argumentos)? VistaParaOJoalheiro = null,
    string? ModeloPadrao = null);
