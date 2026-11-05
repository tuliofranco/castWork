namespace Castwork.Domain.Common;

/// <summary>
/// Entidade que é desativada em vez de excluída (ex.: cliente com histórico de peças).
/// Validações de existência (<c>MustExists</c>) consideram só as ativas.
/// </summary>
public interface IAtivavel
{
    bool Ativo { get; }
}
