using Castwork.Domain.Entities.Briefings;

namespace Castwork.Application.Abstractions;

/// <summary>
/// Uma mensagem como a conversa a guarda: <see cref="Conteudo"/> é opaco para a Application.
/// </summary>
public sealed record MensagemDaConversa(PapelMensagem Papel, string? Texto, string Conteudo);
