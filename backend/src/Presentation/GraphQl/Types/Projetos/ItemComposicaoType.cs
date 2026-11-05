using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Types.Pecas;

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
        descriptor.Field(i => i.PecaId);

        descriptor.Field("peca")
            .Type<NonNullType<PecaType>>()
            .Resolve(ctx => ctx.DataLoader<PecaPorIdDataLoader>().LoadRequiredAsync(ctx.Parent<ItemComposicao>().PecaId, ctx.RequestAborted));
    }
}
