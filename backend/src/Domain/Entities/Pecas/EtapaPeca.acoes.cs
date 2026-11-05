namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Ações da <see cref="EtapaPeca"/>. Só o agregado <see cref="Peca"/> as chama (internal).
/// </summary>
public sealed partial class EtapaPeca
{
    internal void Concluir(DateTimeOffset em) => ConcluidaEm = em;
}
