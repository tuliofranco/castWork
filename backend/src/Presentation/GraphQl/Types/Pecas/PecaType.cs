using Castwork.Domain.Entities.Pecas;

namespace Castwork.GraphQl.Types.Pecas;

/// <summary>
/// Peça da biblioteca. Campos declarados um a um; value objects viram tipos simples na borda.
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
        descriptor.Field(p => p.Material);
        descriptor.Field(p => p.Descricao);
        descriptor.Field(p => p.Ativo);
        descriptor.Field(p => p.CriadoEm);

        descriptor.Field("teor")
            .Type<IntType>()
            .Description("Teor em milésimos (750 = 18k), quando a peça é de metal.")
            .Resolve(ctx => ctx.Parent<Peca>().Teor?.Milesimos);

        descriptor.Field("pesoGramas")
            .Type<DecimalType>()
            .Resolve(ctx => ctx.Parent<Peca>().Peso?.Gramas);
    }
}
