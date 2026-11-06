namespace Castwork.Application.Abstractions;

/// <summary>
/// Uma imagem da conversa, como vai para a IA: a chave liga a imagem à mensagem em que foi enviada.
/// </summary>
public sealed record ImagemDaConversa(Guid Chave, string Tipo, byte[] Conteudo);
