using Castwork.Domain.Entities.Assistente;

namespace Castwork.Application.Abstractions;

/// <summary>
/// Uma mensagem como a conversa a guarda: <see cref="Conteudo"/> é opaco para a Application.
/// </summary>
public sealed record MensagemDaConversa(PapelMensagem Papel, string? Texto, string Conteudo);
