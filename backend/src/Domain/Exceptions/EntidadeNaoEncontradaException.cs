namespace Castwork.Domain.Exceptions;

/// <summary>
/// Entidade inexistente. A API traduz para 404.
/// </summary>
public sealed class EntidadeNaoEncontradaException(string entidade, object id)
    : DomainException($"{entidade} '{id}' não encontrado(a).")
{
    public string Entidade { get; } = entidade;

    public object Id { get; } = id;
}
