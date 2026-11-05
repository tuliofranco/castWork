using Castwork.Domain.Entities.Clientes;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Types.Projetos;

namespace Castwork.GraphQl.Types.Clientes;

public sealed class ClienteType : ObjectType<Cliente>
{
    protected override void Configure(IObjectTypeDescriptor<Cliente> descriptor)
    {
        descriptor.Name("Cliente");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(c => c.Id);
        descriptor.Field(c => c.Nome);
        descriptor.Field(c => c.Email);
        descriptor.Field(c => c.Telefone);
        descriptor.Field(c => c.Ativo);
        descriptor.Field(c => c.CriadoEm);

        // Value object na borda: o front recebe o CPF/CNPJ já com máscara.
        descriptor.Field("documento")
            .Type<StringType>()
            .Resolve(ctx => ctx.Parent<Cliente>().Documento?.Formatado);

        descriptor.Field("projetos")
            .Description("Projetos do cliente, do mais recente para o mais antigo.")
            .Type<NonNullType<ListType<NonNullType<ProjetoType>>>>()
            .Resolve(async ctx =>
                (await ctx.DataLoader<ProjetosPorClienteDataLoader>().LoadAsync(ctx.Parent<Cliente>().Id, ctx.RequestAborted)) ?? []);
    }
}
