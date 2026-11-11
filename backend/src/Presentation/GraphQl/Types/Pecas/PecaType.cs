using Castwork.Application.Pecas.Queries;
using Castwork.Domain.Entities.Pecas;
using Castwork.GraphQl.DataLoaders;

using HotChocolate.Authorization;

namespace Castwork.GraphQl.Types.Pecas;

/// <summary>
/// Peça da biblioteca (sem valor: o valor é de cada projeto). Campos declarados um a um; value objects viram tipos simples
/// na borda.
/// </summary>
public sealed class PecaType : ObjectType<Peca>
{
    protected override void Configure(IObjectTypeDescriptor<Peca> descriptor)
    {
        descriptor.Name("Peca");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Nome);
        descriptor.Field(p => p.Tipo);
        descriptor.Field(p => p.LigaId).Description("A liga do ateliê escolhida (peças de metal); null na pedra, sem liga ou nas peças anteriores à liga.");
        descriptor.Field(p => p.Material).Description("Nome da liga copiado quando ela foi escolhida, ou o material livre da pedra.");
        descriptor.Field(p => p.Descricao);
        descriptor.Field(p => p.Ativo);
        descriptor.Field(p => p.CriadoEm);

        descriptor.Field("teor")
            .Type<IntType>()
            .Description("Teor em milésimos (750 = 18k), copiado da liga; null na pedra.")
            .Resolve(ctx => ctx.Parent<Peca>().Teor?.Milesimos);

        descriptor.Field("pesoGramas")
            .Type<DecimalType>()
            .Resolve(ctx => ctx.Parent<Peca>().Peso?.Gramas);

        descriptor.Field("desenhos")
            .Type<NonNullType<ListType<NonNullType<ObjectType<DesenhoDaPecaDto>>>>>()
            .Description("O desenho 2D e o modelo 3D da peça, copiados do projeto ao publicar (só para ver). O conteúdo vem por REST.")
            .Resolve(async ctx => (await ctx.DataLoader<DesenhosPorPecaDataLoader>().LoadAsync(ctx.Parent<Peca>().Id, ctx.RequestAborted)) ?? []);

        // Diz em que projetos a peça está: exige ler projetos. Nulo no schema: sem a regra, só o campo some (com o erro).
        descriptor.Field("historicoDeUso")
            .Type<ListType<NonNullType<UsoDaPecaType>>>()
            .Description("Projetos que usam a peça (pelas cópias vinculadas a ela), com as versões e o valor em cada projeto.")
            .Authorize(Politicas.ProjetosLer)
            .Resolve(async ctx => (await ctx.DataLoader<HistoricoDeUsoPorPecaDataLoader>().LoadAsync(ctx.Parent<Peca>().Id, ctx.RequestAborted)) ?? []);
    }
}
