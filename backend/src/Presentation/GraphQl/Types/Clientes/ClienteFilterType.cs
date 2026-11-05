using Castwork.Domain.Entities.Clientes;

using HotChocolate.Data.Filters;

namespace Castwork.GraphQl.Types.Clientes;

public sealed class ClienteFilterType : FilterInputType<Cliente>
{
    protected override void Configure(IFilterInputTypeDescriptor<Cliente> descriptor)
    {
        descriptor.Name("ClienteFiltro");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(c => c.Nome);
        descriptor.Field(c => c.Ativo);
        descriptor.Field(c => c.CriadoEm);
    }
}
