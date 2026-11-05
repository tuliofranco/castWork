using Castwork.Domain.Entities.Pecas;

using HotChocolate.Data.Filters;

namespace Castwork.GraphQl.Types.Pecas;

/// <summary>
/// Só os campos filtráveis (e indexados ou baratos). Sem isto, o HotChocolate exporia filtro em todo campo.
/// </summary>
public sealed class PecaFilterType : FilterInputType<Peca>
{
    protected override void Configure(IFilterInputTypeDescriptor<Peca> descriptor)
    {
        descriptor.Name("PecaFiltro");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Titulo);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.ClienteId);
        descriptor.Field(p => p.PrazoEntrega);
        descriptor.Field(p => p.CriadoEm);
    }
}
