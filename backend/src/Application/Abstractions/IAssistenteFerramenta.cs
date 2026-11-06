using Castwork.Domain.Entities.Briefings;

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
/// a fala do joalheiro com as imagens anexadas, e o arquivo atual da ferramenta (nulo se ainda não existe).
/// </summary>
public sealed record PedidoAFerramenta(
    string Ferramenta,
    string Contexto,
    IReadOnlyList<MensagemDaConversa> Historico,
    string? Fala,
    IReadOnlyList<Guid> ImagensDaFala,
    IReadOnlyDictionary<Guid, ImagemDaConversa> Imagens,
    byte[]? Arquivo);

/// <summary>
/// O que o turno produziu. <see cref="ArquivoNovo"/> só vem quando a IA mudou o arquivo.
/// </summary>
public sealed record TurnoDaFerramenta(
    IReadOnlyList<MensagemDoTurno> Mensagens,
    IReadOnlyList<ImagemDaConversa> ImagensGeradas,
    byte[]? ArquivoNovo);

/// <summary>
/// Uma mensagem nova. <see cref="Acao"/> resume o que a IA mudou no arquivo (o cartão com Desfazer do chat).
/// </summary>
public sealed record MensagemDoTurno(PapelMensagem Papel, string? Texto, string Conteudo, IReadOnlyList<Guid> Imagens, string? Acao);
