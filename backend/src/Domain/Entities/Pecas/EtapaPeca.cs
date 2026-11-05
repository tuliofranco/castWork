using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Passagem da peça por um status: quando entrou e quando saiu. Pertence ao agregado <see cref="Peca"/>
/// e é onde as fotos de portfólio de cada etapa vão se pendurar.
/// </summary>
public sealed partial class EtapaPeca : BaseEntity<long>
{
    internal EtapaPeca(StatusPeca status, DateTimeOffset iniciadaEm)
    {
        Status = status;
        IniciadaEm = iniciadaEm;
    }

    private EtapaPeca()
    {
    }

    public StatusPeca Status { get; private set; }

    public DateTimeOffset IniciadaEm { get; private set; }

    public DateTimeOffset? ConcluidaEm { get; private set; }

    public bool EmAndamento => ConcluidaEm is null;
}
