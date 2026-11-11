using Castwork.Application.Pecas.Queries;

using HotChocolate.Authorization;

namespace Castwork.GraphQl.Types.Pecas;

/// <summary>
/// Um projeto que usa a peça da biblioteca. O valor no projeto é dado de precificação.
/// </summary>
public sealed class UsoDaPecaType : ObjectType<UsoDaPecaDto>
{
    protected override void Configure(IObjectTypeDescriptor<UsoDaPecaDto> descriptor)
    {
        descriptor.Name("UsoDaPeca");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(u => u.ProjetoId);
        descriptor.Field(u => u.Projeto);
        descriptor.Field(u => u.PecaDoProjetoId);
        descriptor.Field(u => u.Versoes).Description("Versões cuja composição usa a peça (vazia se o projeto ainda não a usou).");
        descriptor.Field(u => u.Valor)
            .Description("Valor da peça neste projeto (custo de uma unidade, R$); null sem valor.")
            .Authorize(Politicas.PrecificacaoLer);
    }
}
