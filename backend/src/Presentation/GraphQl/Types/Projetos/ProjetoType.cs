using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Types.Clientes;

namespace Castwork.GraphQl.Types.Projetos;

public sealed class ProjetoType : ObjectType<Projeto>
{
    protected override void Configure(IObjectTypeDescriptor<Projeto> descriptor)
    {
        descriptor.Name("Projeto");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Nome);
        descriptor.Field(p => p.Descricao);
        descriptor.Field(p => p.ClienteId);
        descriptor.Field(p => p.CriadoEm);
        descriptor.Field(p => p.AlteradoEm);

        descriptor.Field("cliente")
            .Type<ClienteType>()
            .Resolve(ctx => ctx.Parent<Projeto>().ClienteId is long id
                ? ctx.DataLoader<ClientePorIdDataLoader>().LoadAsync(id, ctx.RequestAborted)
                : Task.FromResult<Domain.Entities.Clientes.Cliente?>(null));

        // Preços por DataLoader (uma consulta para todos os projetos pedidos). Os valores ficam protegidos no tipo.
        descriptor.Field("precos")
            .Description("Preços deste projeto: cópia dos do ateliê, editável só aqui.")
            .Type<NonNullType<PrecosDoProjetoType>>()
            .Resolve(async ctx => await ctx.DataLoader<PrecosPorProjetoDataLoader>().LoadRequiredAsync(ctx.Parent<Projeto>().Id, ctx.RequestAborted));

        // Versões vêm por DataLoader: na lista de projetos, uma consulta para todas as versões de todos eles.
        // O "id" opcional restringe a uma versão (a página da versão pede o preço só dela).
        descriptor.Field("versoes")
            .Description("Versões da arte, da mais recente para a mais antiga.")
            .Argument("id", a => a.Type<LongType>().Description("Só a versão com este Id."))
            .Type<NonNullType<ListType<NonNullType<VersaoProjetoType>>>>()
            .Resolve(async ctx =>
            {
                var versoes = (await ctx.DataLoader<VersoesPorProjetoDataLoader>().LoadAsync(ctx.Parent<Projeto>().Id, ctx.RequestAborted)) ?? [];
                return ctx.ArgumentValue<long?>("id") is long id ? [.. versoes.Where(v => v.Id == id)] : versoes;
            });
    }
}
