using Castwork.Domain.Eventos;

namespace Castwork.Domain.Entities.Pecas.Eventos;

/// <summary>
/// Base de todos os eventos de transição. Um handler pode assinar a base (ex.: linha do tempo, auditoria)
/// ou só a transição que lhe interessa (ex.: <see cref="PecaAprovadaEvent"/> → gerar a ordem de serviço).
/// </summary>
public abstract record StatusPecaAlteradoEvent(Peca Peca, StatusPeca De, StatusPeca Para, DateTimeOffset OcorridoEm)
    : DomainEvent(OcorridoEm);
