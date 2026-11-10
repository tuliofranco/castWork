using Castwork.Domain.Entities.Assistente;

namespace Castwork.Application.Abstractions;

/// <summary>
/// A IA das ferramentas de trabalho (desenho 2D, e as próximas). Cada ferramenta tem as próprias instruções
/// e o próprio servidor MCP; a implementação (Infra) sabe ligar um ao outro. A Application guarda o que
/// volta: as mensagens, as imagens que a IA gerou e o arquivo novo.
/// </summary>
public interface IAssistenteFerramenta
{
    /// <summary>
    /// Ferramentas que esta instalação sabe conduzir (código da rota, ex.: <c>desenho-2d</c>).
    /// </summary>
    IReadOnlyCollection<string> Ferramentas { get; }

    Task<TurnoDaFerramenta> ConversarAsync(PedidoAFerramenta pedido, CancellationToken cancellationToken);
}

/// <summary>
/// Um turno da conversa: o contexto montado pela Application (projeto, versão, briefing), o histórico,
/// a fala do joalheiro com as imagens e os arquivos do projeto anexados, e o arquivo atual da ferramenta (nulo se ainda não existe).
/// <see cref="Observar"/> recebe os eventos do turno ao vivo (pensamento, passos e texto). <see cref="Locais"/>:
/// ferramentas do próprio Castwork que a IA também pode usar (as do briefing). Sem fala nem imagem, a IA abre a
/// conversa (a mensagem de abertura fica oculta no chat). <see cref="Modelo"/> e <see cref="Esforco"/>: a escolha
/// do joalheiro no chat (nulos usam o padrão da configuração). <see cref="PreparoDoArquivo"/>: o que a ferramenta pede que
/// o arquivo tenha ao ser aberto (ex.: os materiais do modelo 3D), garantido antes da IA começar.
/// <see cref="EscopoDaVersao"/>: a conversa é de uma versão (não de uma peça nem do projeto); a ferramenta pode limitar o
/// que a IA faz ali (no modelo 3D da versão, só montar e posicionar as peças).
/// </summary>
public sealed record PedidoAFerramenta(
    string Ferramenta,
    string Contexto,
    IReadOnlyList<MensagemDaConversa> Historico,
    string? Fala,
    IReadOnlyList<Guid> ImagensDaFala,
    IReadOnlyDictionary<Guid, ImagemDaConversa> Imagens,
    byte[]? Arquivo,
    Action<EventoDoTurno>? Observar = null,
    IFerramentasLocais? Locais = null,
    IReadOnlyList<AnexoDoTurno>? Anexos = null,
    string? Modelo = null,
    string? Esforco = null,
    string? PreparoDoArquivo = null,
    bool EscopoDaVersao = false)
{
    public static readonly IReadOnlyList<string> Modelos = ["haiku", "sonnet", "opus"];

    public static readonly IReadOnlyList<string> Esforcos = ["low", "medium", "high"];
}

/// <summary>
/// Um arquivo do projeto anexado à fala, com o conteúdo. <see cref="Extensao"/> nula: a da ferramenta que o
/// gravou (<see cref="Ferramenta"/>).
/// </summary>
public sealed record AnexoDoTurno(string Nome, string Descricao, string Ferramenta, string? Extensao, byte[] Conteudo);

/// <summary>
/// O que o turno produziu. <see cref="ArquivoNovo"/> só vem quando a IA mudou o arquivo.
/// </summary>
public sealed record TurnoDaFerramenta(
    IReadOnlyList<MensagemDoTurno> Mensagens,
    IReadOnlyList<ImagemDaConversa> ImagensGeradas,
    byte[]? ArquivoNovo,
    ConsumoDoTurno? Consumo = null);

/// <summary>
/// Uma mensagem nova. <see cref="Acao"/> resume o que a IA mudou no arquivo (o cartão com Desfazer do chat).
/// A resposta final traz também o resumo do pensamento e os passos do turno.
/// </summary>
public sealed record MensagemDoTurno(
    PapelMensagem Papel,
    string? Texto,
    string Conteudo,
    IReadOnlyList<Guid> Imagens,
    string? Acao,
    string? Pensamento = null,
    IReadOnlyList<string>? Passos = null);

/// <summary>
/// Tokens somados de todas as chamadas do turno, como a API informou em cada resposta.
/// </summary>
public sealed record ConsumoDoTurno(string Modelo, int Chamadas, int Entrada, int Saida, int CacheLeitura, int CacheEscrita)
{
    public int Total => Entrada + Saida + CacheLeitura + CacheEscrita;
}
