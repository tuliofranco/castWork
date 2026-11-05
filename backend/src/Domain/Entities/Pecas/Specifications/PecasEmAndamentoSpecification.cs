using Castwork.Domain.Specifications;

namespace Castwork.Domain.Entities.Pecas.Specifications;

/// <summary>
/// Peças ainda na bancada (nem entregues nem canceladas), com o prazo mais próximo primeiro.
/// Opcionalmente restrita a um cliente.
/// </summary>
public sealed class PecasEmAndamentoSpecification : BaseSpecification<Peca>
{
    public PecasEmAndamentoSpecification(long? clienteId = null)
        : base(p => p.Status != StatusPeca.Entregue
            && p.Status != StatusPeca.PosVenda
            && p.Status != StatusPeca.Cancelada
            && (clienteId == null || p.ClienteId == clienteId))
        => Ordenar(p => p.PrazoEntrega ?? DateOnly.MaxValue);
}
