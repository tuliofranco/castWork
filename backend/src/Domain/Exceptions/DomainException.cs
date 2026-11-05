namespace Castwork.Domain.Exceptions;

/// <summary>
/// Base de toda exceção de regra de negócio. A API traduz para 422 (Unprocessable Entity),
/// exceto as especializações mapeadas para outro status (ex.: não encontrado → 404).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
