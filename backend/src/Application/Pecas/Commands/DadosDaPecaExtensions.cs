using Castwork.Domain.ValueObjects;

namespace Castwork.Application.Pecas.Commands;

/// <summary>
/// Converte os campos validados de <see cref="DadosDaPeca"/> nos value objects do domínio.
/// </summary>
internal static class DadosDaPecaExtensions
{
    public static TeorMetal? TeorDominio(this DadosDaPeca dados)
        => string.IsNullOrWhiteSpace(dados.Teor) ? null : TeorMetal.Parse(dados.Teor);

    public static Peso? PesoDominio(this DadosDaPeca dados)
        => dados.PesoGramas is null ? null : Peso.EmGramas(dados.PesoGramas.Value);

    public static Dinheiro? CustoDominio(this DadosDaPeca dados)
        => dados.Custo is null ? null : Dinheiro.Criar(dados.Custo.Value);
}
