using Castwork.Domain.Entities.Projetos;

using HotChocolate.Data.Sorting;

namespace Castwork.GraphQl.Types.Projetos;

public sealed class ProjetoSortType : SortInputType<Projeto>
{
    protected override void Configure(ISortInputTypeDescriptor<Projeto> descriptor)
    {
        descriptor.Name("ProjetoOrdenacao");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Nome);
        descriptor.Field(p => p.CriadoEm);
    }
}
