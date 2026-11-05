using Castwork.Domain.Eventos;

namespace Castwork.Domain.Entities.Pecas.Eventos;

public sealed record PecaCriadaEvent(Peca Peca, DateTimeOffset OcorridoEm) : DomainEvent(OcorridoEm);
