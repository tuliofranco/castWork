namespace Castwork.Domain.Exceptions;

/// <summary>
/// Valor que viola a invariante de uma entidade ou value object.
/// </summary>
public sealed class ValorInvalidoException(string campo, string motivo)
    : DomainException($"{campo} {motivo}.")
{
    public string Campo { get; } = campo;
}
