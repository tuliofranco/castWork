using Castwork.Domain.Entities.Pecas;

using HotChocolate.Data.Filters;

namespace Castwork.GraphQl.Types.Pecas;

public sealed class PecaFilterType : FilterInputType<Peca>
{
    protected override void Configure(IFilterInputTypeDescriptor<Peca> descriptor)
    {
        descriptor.Name("PecaFiltro");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Tipo);
        descriptor.Field(p => p.Ativo);
        descriptor.Field(p => p.CriadoEm);
    }
}
