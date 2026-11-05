using Castwork.Domain.Entities.Projetos;

using HotChocolate.Data.Filters;

namespace Castwork.GraphQl.Types.Projetos;

public sealed class ProjetoFilterType : FilterInputType<Projeto>
{
    protected override void Configure(IFilterInputTypeDescriptor<Projeto> descriptor)
    {
        descriptor.Name("ProjetoFiltro");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.ClienteId);
        descriptor.Field(p => p.CriadoEm);
    }
}
