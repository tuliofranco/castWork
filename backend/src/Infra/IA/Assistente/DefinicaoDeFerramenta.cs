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
/// nulo usa o da configuração. <see cref="PrepararArquivo"/>: depois de abrir o arquivo, com o preparo que a Application
/// mandou no pedido, a chamada que lê o arquivo e a que monta, a partir dessa leitura, o que falta nele (nula: nada
/// falta); ex.: os materiais das ligas no modelo 3D (<see cref="PreparoDoArquivo"/>). <see cref="VistasGuardadas"/>: as imagens
/// em alta qualidade geradas em segundo plano para cada arquivo novo (<see cref="GeradorDeImagensMcp"/>); nulo, a ferramenta
/// não guarda imagens. <see cref="RecusarNaVersao"/>: na conversa de uma versão, o motivo de recusar a chamada que a IA fez
/// (o nome da ferramenta do MCP e a entrada) sem passá-la ao MCP; nulo deixa passar (ex.: o modelo 3D da versão não cria
/// corpos).
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
    string? ModeloPadrao = null,
    ((string Nome, string Argumentos) Leitura, Func<string, string, (string Nome, string Argumentos)?> Ajustar)? PrepararArquivo = null,
    IReadOnlyList<VistaGuardada>? VistasGuardadas = null,
    Func<string, IReadOnlyDictionary<string, System.Text.Json.JsonElement>, string?>? RecusarNaVersao = null);

/// <summary>
/// Uma imagem guardada do arquivo: a vista (<c>VistaDaImagem</c>), a extensão do arquivo que o programa grava e a chamada
/// que a gera, com o caminho onde gravar. A imagem é a que o programa devolve no resultado ou, se não devolve, o arquivo
/// gravado no caminho (o SVG do 2D).
/// </summary>
internal sealed record VistaGuardada(string Vista, string Extensao, Func<string, (string Nome, string Argumentos)> Chamada);
