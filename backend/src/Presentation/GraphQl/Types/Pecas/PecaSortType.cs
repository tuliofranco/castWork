using Castwork.Domain.Entities.Pecas;

using HotChocolate.Data.Sorting;

namespace Castwork.GraphQl.Types.Pecas;

public sealed class PecaSortType : SortInputType<Peca>
{
    protected override void Configure(ISortInputTypeDescriptor<Peca> descriptor)
    {
        descriptor.Name("PecaOrdenacao");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Titulo);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.PrazoEntrega);
        descriptor.Field(p => p.CriadoEm);
    }
}
