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

        // Versões vêm por DataLoader: na lista de projetos, uma consulta para todas as versões de todos eles.
        descriptor.Field("versoes")
            .Description("Versões da arte, da mais recente para a mais antiga.")
            .Type<NonNullType<ListType<NonNullType<VersaoProjetoType>>>>()
            .Resolve(async ctx =>
                (await ctx.DataLoader<VersoesPorProjetoDataLoader>().LoadAsync(ctx.Parent<Projeto>().Id, ctx.RequestAborted)) ?? []);
    }
}
