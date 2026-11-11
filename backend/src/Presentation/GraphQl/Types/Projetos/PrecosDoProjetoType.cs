using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Application.Projetos.Queries;

using HotChocolate.Authorization;

namespace Castwork.GraphQl.Types.Projetos;

/// <summary>
/// Preços do projeto. As datas são livres para quem lê o projeto; cotações e parâmetros (markup inclusive) são dado de
/// precificação: sem <c>precificacao.ler</c> o campo responde erro de autorização e o valor não sai.
/// </summary>
public sealed class PrecosDoProjetoType : ObjectType<PrecosDoProjetoDto>
{
    protected override void Configure(IObjectTypeDescriptor<PrecosDoProjetoDto> descriptor)
    {
        descriptor.Name("PrecosDoProjeto");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.HerdadosEm).Description("Quando os preços foram copiados do ateliê.");
        descriptor.Field(p => p.AlteradosEm).Description("Quando foram editados no projeto; null enquanto estão como herdados.");

        // Nulos no schema: sem a regra, só o campo some (com o erro), e não o projeto inteiro.
        descriptor.Field(p => p.Cotacoes)
            .Description("R$/g do metal puro no projeto, só os metais com cotação.")
            .Type<ListType<NonNullType<ObjectType<CotacaoDoProjetoDto>>>>()
            .Authorize(Politicas.PrecificacaoLer);
        descriptor.Field(p => p.Parametros)
            .Description("Perda, valor da hora, markup, taxas e arredondamento do projeto.")
            .Type<ObjectType<ParametrosDePrecoDto>>()
            .Authorize(Politicas.PrecificacaoLer);
    }
}
