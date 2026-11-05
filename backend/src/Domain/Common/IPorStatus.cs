namespace Castwork.Domain.Common;

/// <summary>
/// Entidade governada por um status. Permite filtros e behaviours genéricos sobre o status.
/// </summary>
public interface IPorStatus<out TStatus>
    where TStatus : struct, Enum
{
    TStatus Status { get; }
}
