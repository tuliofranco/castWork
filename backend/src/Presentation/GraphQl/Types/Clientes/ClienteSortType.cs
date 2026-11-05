using Castwork.Domain.Entities.Clientes;

using HotChocolate.Data.Sorting;

namespace Castwork.GraphQl.Types.Clientes;

public sealed class ClienteSortType : SortInputType<Cliente>
{
    protected override void Configure(ISortInputTypeDescriptor<Cliente> descriptor)
    {
        descriptor.Name("ClienteOrdenacao");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(c => c.Id);
        descriptor.Field(c => c.Nome);
        descriptor.Field(c => c.CriadoEm);
    }
}
