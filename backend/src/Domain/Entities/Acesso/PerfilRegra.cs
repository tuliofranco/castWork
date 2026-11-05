using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Acesso;

public sealed class PerfilRegra : BaseEntity<long>
{
    internal PerfilRegra(Guid regraId) => RegraId = regraId;

    private PerfilRegra()
    {
    }

    public Guid RegraId { get; private set; }
}
