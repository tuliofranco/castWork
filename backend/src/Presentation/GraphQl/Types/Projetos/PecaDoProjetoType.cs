using Castwork.Application.Projetos.Queries;
using Castwork.Application.Projetos.Queries.GetPesoDaPeca;
using Castwork.Domain.Entities.Projetos;
using Castwork.GraphQl.DataLoaders;

using HotChocolate.Authorization;

namespace Castwork.GraphQl.Types.Projetos;

/// <summary>
/// Peça do projeto: os dados da peça, o status em relação à biblioteca, os arquivos finais do desenho e o valor no projeto
/// (dado de precificação).
/// </summary>
public sealed class PecaDoProjetoType : ObjectType<PecaDoProjeto>
{
    protected override void Configure(IObjectTypeDescriptor<PecaDoProjeto> descriptor)
    {
        descriptor.Name("PecaDoProjeto");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Nome);
        descriptor.Field(p => p.Tipo);
        descriptor.Field(p => p.LigaId).Description("A liga do ateliê escolhida (peças de metal); null na pedra, sem liga ou nas peças anteriores à liga.");
        descriptor.Field(p => p.Material).Description("Nome da liga copiado quando ela foi escolhida, ou o material livre da pedra.");
        descriptor.Field(p => p.Descricao);
        descriptor.Field(p => p.Ativo);
        descriptor.Field(p => p.Status).Description("RASCUNHO: só neste projeto; PUBLICADA: está na biblioteca.");
        descriptor.Field(p => p.PecaId).Description("A peça da biblioteca de que esta é cópia; null em rascunho.");

        descriptor.Field("teor")
            .Type<IntType>()
            .Description("Teor em milésimos (750 = 18k), copiado da liga; null na pedra.")
            .Resolve(ctx => ctx.Parent<PecaDoProjeto>().Teor?.Milesimos);

        descriptor.Field("pesoGramas")
            .Type<DecimalType>()
            .Resolve(ctx => ctx.Parent<PecaDoProjeto>().Peso?.Gramas);

        // Mede o modelo 3D da peça (medição guardada pelo arquivo): pensado para a página da peça e a da versão.
        descriptor.Field("peso")
            .Type<NonNullType<ObjectType<PesoDaPecaDto>>>()
            .Description("O peso que vale para a peça e de onde vem: medido no modelo 3D (na liga da peça) ou, sem 3D, o digitado.")
            .Resolve(async ctx => await ctx.DataLoader<PesoPorPecaDoProjetoDataLoader>().LoadRequiredAsync(ctx.Parent<PecaDoProjeto>().Id, ctx.RequestAborted));

        descriptor.Field("arquivosFinais")
            .Type<NonNullType<ListType<NonNullType<ObjectType<ArquivoFinalDaPecaDto>>>>>()
            .Description("O arquivo final de cada ferramenta da peça (desenho 2D e modelo 3D), o que vale para o peso, a montagem e a publicação; a ferramenta sem final não aparece.")
            .Resolve(async ctx => (await ctx.DataLoader<ArquivosFinaisPorPecaDoProjetoDataLoader>().LoadAsync(ctx.Parent<PecaDoProjeto>().Id, ctx.RequestAborted)) ?? []);

        // O valor é dado de precificação: sem a regra, o campo responde erro de autorização e o valor não sai.
        descriptor.Field("valor")
            .Type<DecimalType>()
            .Description("Custo de uma unidade neste projeto (R$), não o preço de venda. Null quando ainda não tem valor.")
            .Authorize(Politicas.PrecificacaoLer)
            .Resolve(ctx => ctx.Parent<PecaDoProjeto>().Valor?.Valor);
    }
}
