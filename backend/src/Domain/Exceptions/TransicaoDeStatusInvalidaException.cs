namespace Castwork.Domain.Exceptions;

/// <summary>
/// Tentativa de mover uma entidade para um status não permitido a partir do status atual.
/// </summary>
public sealed class TransicaoDeStatusInvalidaException(string entidade, Enum de, Enum para)
    : DomainException($"{entidade} não pode passar de '{de}' para '{para}'.")
{
    public string De { get; } = de.ToString();

    public string Para { get; } = para.ToString();
}
