using Castwork.Domain.Entities.Assistente;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// A conversa da ferramenta como o chat mostra, e a versão atual do arquivo (nula se ainda não existe).
/// O front compara <see cref="ArquivoId"/> para saber quando recarregar o desenho. <see cref="Processando"/>:
/// a IA ainda trabalha na última fala (o front espera e consulta de novo). <see cref="Chat"/>: o número do
/// chat mostrado; <see cref="ChatsAnteriores"/>: quantos outros chats estão no histórico.
/// </summary>
public sealed record ConversaFerramentaDto(IReadOnlyList<MensagemFerramentaDto> Mensagens, long? ArquivoId, bool Processando, int Chat = 1, int ChatsAnteriores = 0);

/// <summary>
/// Um chat da conversa, para a lista de conversas anteriores: o título é o começo da primeira fala.
/// </summary>
public sealed record ChatDaConversaDto(int Numero, DateTimeOffset IniciadoEm, DateTimeOffset UltimaMensagemEm, string? Titulo, int Mensagens, bool Atual);

/// <summary>
/// Uma mensagem do chat. Na resposta final da IA vêm também o resumo do pensamento, os passos do turno e
/// os tokens consumidos.
/// </summary>
public sealed record MensagemFerramentaDto(
    int Ordem,
    PapelMensagem Papel,
    string? Texto,
    IReadOnlyList<Guid> Imagens,
    AcaoDaIaDto? Acao,
    DateTimeOffset CriadaEm,
    string? Pensamento = null,
    IReadOnlyList<string>? Passos = null,
    int? Tokens = null);

public sealed record AcaoDaIaDto(string Descricao, bool Desfeita);

public sealed record ArquivoDaFerramentaDto(long Id, byte[] Conteudo, string? Nome = null);

public sealed record ImagemDaFerramentaDto(string Tipo, byte[] Conteudo);

/// <summary>
/// Uma versão do histórico do arquivo; <see cref="Tamanho"/> 0 é o desenho em branco.
/// </summary>
public sealed record VersaoDoArquivoDto(long Id, Domain.Entities.Assistente.OrigemArquivo Origem, DateTimeOffset CriadoEm, int Tamanho, string? Nome);

/// <summary>
/// O SVG do arquivo atual; <see cref="ArquivoId"/> diz de qual versão do arquivo ele é.
/// </summary>
public sealed record PreviaDoArquivoDto(long ArquivoId, byte[] Svg);

/// <summary>
/// Um arquivo da pasta da versão: de qual ferramenta é (<c>desenho-2d</c>), quem gravou e quando.
/// Propriedades com init (não posicional): a paginação filtra e ordena pelo DTO, e o EF só traduz isso assim.
/// </summary>
public sealed record ArquivoDaVersaoDto
{
    public long Id { get; init; }

    public string Ferramenta { get; init; } = string.Empty;

    public Domain.Entities.Assistente.OrigemArquivo Origem { get; init; }

    public DateTimeOffset CriadoEm { get; init; }

    public int Tamanho { get; init; }

    /// <summary>
    /// Nome dado pelo joalheiro; nulo usa o padrão da ferramenta.
    /// </summary>
    public string? Nome { get; init; }

    public DateTimeOffset? ArquivadoEm { get; init; }
}
