namespace Castwork.Domain.Eventos;

/// <summary>
/// Algo relevante que aconteceu no domínio. Não herda de <c>INotification</c> do MediatR para manter
/// o Domain sem dependências: a Infra embrulha o evento numa notificação na hora de publicar.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OcorridoEm { get; }
}
