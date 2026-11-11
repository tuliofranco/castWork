using Castwork.Application.Projetos.Queries.GetPrecoDaVersao;

using HotChocolate.Authorization;

namespace Castwork.GraphQl.Types.Projetos;

/// <summary>
/// Preço da versão, calculado na leitura. Preço e status são livres para quem lê o projeto; o cálculo (custos, markup,
/// cotações, pendências e avisos) é dado de precificação: sem <c>precificacao.ler</c> o campo responde erro de
/// autorização e o valor não sai (a query também não o calcula para a borda).
/// </summary>
public sealed class PrecoDaVersaoType : ObjectType<PrecoDaVersaoDto>
{
    protected override void Configure(IObjectTypeDescriptor<PrecoDaVersaoDto> descriptor)
    {
        descriptor.Name("PrecoDaVersao");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(p => p.Preco).Description("Preço final (R$), arredondado para cima. Parcial quando o status é INCOMPLETO.");
        descriptor.Field(p => p.Status).Description("COMPLETO, ou INCOMPLETO enquanto houver pendência.");
        descriptor.Field(p => p.Calculo)
            .Description("O cálculo linha a linha e os dados do orçamento da versão.")
            .Type<ObjectType<CalculoDoPrecoDto>>()
            .Authorize(Politicas.PrecificacaoLer);
    }
}
