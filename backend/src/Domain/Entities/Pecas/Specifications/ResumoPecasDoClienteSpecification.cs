using Castwork.Domain.Specifications;

namespace Castwork.Domain.Entities.Pecas.Specifications;

/// <summary>
/// Histórico de peças do cliente projetado só com o necessário para a ficha do cliente.
/// </summary>
public sealed class ResumoPecasDoClienteSpecification : BaseSpecification<Peca, ResumoPeca>
{
    public ResumoPecasDoClienteSpecification(long clienteId)
        : base(p => new ResumoPeca(p.Id, p.Titulo, p.Status, p.CriadoEm, p.EntregueEm), p => p.ClienteId == clienteId)
        => OrdenarDescendente(p => p.CriadoEm);
}

public sealed record ResumoPeca(long Id, string Titulo, StatusPeca Status, DateTimeOffset CriadoEm, DateTimeOffset? EntregueEm);
