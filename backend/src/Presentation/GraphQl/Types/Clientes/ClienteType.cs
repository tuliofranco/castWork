using Castwork.Domain.Entities.Clientes;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Types.Pecas;

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

        descriptor.Field("pecas")
            .Description("Histórico de peças do cliente, da mais recente para a mais antiga.")
            .Type<NonNullType<ListType<NonNullType<PecaType>>>>()
            .Resolve(async ctx =>
                (await ctx.DataLoader<PecasPorClienteDataLoader>().LoadAsync(ctx.Parent<Cliente>().Id, ctx.RequestAborted)) ?? []);
    }
}
