using Castwork.Domain.Entities.Pecas;
using Castwork.GraphQl.DataLoaders;
using Castwork.GraphQl.Types.Clientes;

namespace Castwork.GraphQl.Types.Pecas;

/// <summary>
/// Campos expostos da peça, declarados um a um: uma propriedade nova no domínio não vaza para a API por acidente.
/// </summary>
public sealed class PecaType : ObjectType<Peca>
{
    protected override void Configure(IObjectTypeDescriptor<Peca> descriptor)
    {
        descriptor.Name("Peca");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Titulo);
        descriptor.Field(p => p.Descricao);
        descriptor.Field(p => p.Status);
        descriptor.Field(p => p.PrazoEntrega);
        descriptor.Field(p => p.EntregueEm);
        descriptor.Field(p => p.MotivoCancelamento);
        descriptor.Field(p => p.ClienteId);
        descriptor.Field(p => p.CriadoEm);
        descriptor.Field(p => p.AlteradoEm);

        descriptor.Field("proximosStatus")
            .Type<NonNullType<ListType<NonNullType<EnumType<StatusPeca>>>>>()
            .Description("Status para os quais a peça pode ir agora (botões habilitados na tela).")
            .Resolve(ctx => TransicoesPeca.ProximosDe(ctx.Parent<Peca>().Status).ToList());

        // N+1: numa lista de 50 peças, buscar o cliente de cada uma seriam 50 consultas.
        // O DataLoader junta os Ids pedidos na mesma rodada e faz uma consulta só (WHERE id IN (...)).
        descriptor.Field("cliente")
            .Type<NonNullType<ClienteType>>()
            .Resolve(ctx => ctx.DataLoader<ClientePorIdDataLoader>().LoadRequiredAsync(ctx.Parent<Peca>().ClienteId, ctx.RequestAborted));
    }
}
