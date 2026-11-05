using Castwork.Domain.Eventos;

namespace Castwork.Domain.Common;

/// <summary>
/// Base de toda entidade. O <see cref="Id"/> é gerado pelo banco e só o EF o atribui (setter privado).
/// Os eventos ficam acumulados aqui até o interceptor do EF despachá-los no <c>SaveChanges</c>.
/// </summary>
public abstract class BaseEntity<TKey>
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TKey Id { get; private set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void LimparEventos() => _domainEvents.Clear();

    protected void AdicionarEvento(IDomainEvent evento) => _domainEvents.Add(evento);
}
