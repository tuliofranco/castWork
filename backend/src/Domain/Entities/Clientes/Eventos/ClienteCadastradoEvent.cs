using Castwork.Domain.Eventos;

namespace Castwork.Domain.Entities.Clientes.Eventos;

/// <summary>
/// Carrega a entidade, e não o Id, porque o Id só existe depois do <c>SaveChanges</c>.
/// </summary>
public sealed record ClienteCadastradoEvent(Cliente Cliente, DateTimeOffset OcorridoEm) : DomainEvent(OcorridoEm);
