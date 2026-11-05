namespace Castwork.Domain.Exceptions;

/// <summary>
/// Violação de regra de negócio sem tipo próprio (ex.: alterar uma peça já entregue).
/// </summary>
public sealed class RegraDeNegocioException(string message) : DomainException(message);
