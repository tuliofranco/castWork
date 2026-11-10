using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.DataLoaders;

namespace Castwork.GraphQl.Types.Projetos;

public sealed class VersaoProjetoType : ObjectType<VersaoProjeto>
{
    protected override void Configure(IObjectTypeDescriptor<VersaoProjeto> descriptor)
    {
        descriptor.Name("VersaoProjeto");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(v => v.Id);
        descriptor.Field(v => v.Numero);
        descriptor.Field(v => v.Nome);
        descriptor.Field(v => v.Observacoes);
        descriptor.Field(v => v.Status);
        descriptor.Field(v => v.PublicadaEm);

        descriptor.Field("itens")
            .Description("Composição: as peças da versão e a quantidade de cada uma.")
            .Type<NonNullType<ListType<NonNullType<ItemComposicaoType>>>>()
            .Resolve(async ctx =>
                (await ctx.DataLoader<ItensPorVersaoDataLoader>().LoadAsync(ctx.Parent<VersaoProjeto>().Id, ctx.RequestAborted)) ?? []);

        // Calculado a cada leitura (nada é gravado): pensado para a página da versão, não para listas.
        descriptor.Field("preco")
            .Description("Preço da versão calculado agora com os preços do projeto. O cálculo detalhado exige precificacao.ler.")
            .Type<NonNullType<PrecoDaVersaoType>>()
            .Resolve(async ctx => await ctx.DataLoader<PrecoPorVersaoDataLoader>().LoadRequiredAsync(ctx.Parent<VersaoProjeto>().Id, ctx.RequestAborted));
    }
}
