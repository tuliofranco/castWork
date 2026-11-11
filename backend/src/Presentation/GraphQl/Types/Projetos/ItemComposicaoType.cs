using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.DataLoaders;

namespace Castwork.GraphQl.Types.Projetos;

public sealed class ItemComposicaoType : ObjectType<ItemComposicao>
{
    protected override void Configure(IObjectTypeDescriptor<ItemComposicao> descriptor)
    {
        descriptor.Name("ItemComposicao");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(i => i.Id);
        descriptor.Field(i => i.Quantidade);
        descriptor.Field(i => i.Observacao);
        descriptor.Field(i => i.PecaDoProjetoId);

        descriptor.Field("peca")
            .Description("A peça do projeto usada nesta linha.")
            .Type<NonNullType<PecaDoProjetoType>>()
            .Resolve(ctx => ctx.DataLoader<PecaDoProjetoPorIdDataLoader>().LoadRequiredAsync(ctx.Parent<ItemComposicao>().PecaDoProjetoId, ctx.RequestAborted));
    }
}
