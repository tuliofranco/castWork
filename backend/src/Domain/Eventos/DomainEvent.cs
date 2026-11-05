namespace Castwork.Domain.Eventos;

/// <summary>
/// Base dos eventos. Records dão imutabilidade e igualdade por valor, úteis em testes e logs.
/// </summary>
public abstract record DomainEvent(DateTimeOffset OcorridoEm) : IDomainEvent;
