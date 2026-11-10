using Castwork.Domain.Entities.Pecas;

namespace Castwork.Application.Pecas.Commands;

/// <summary>
/// Campos de uma peça, comuns a criar e alterar.
/// </summary>
public abstract class DadosDaPeca
{
    public string Nome { get; set; } = string.Empty;

    public TipoPeca Tipo { get; set; }

    public string? Material { get; set; }

    /// <summary>
    /// Teor do metal: "18k" ou "750". Opcional.
    /// </summary>
    public string? Teor { get; set; }

    public decimal? PesoGramas { get; set; }

    public string? Descricao { get; set; }

    /// <summary>
    /// Custo de compra ou produção de uma unidade, em R$ (não é o preço de venda). Opcional. Só é gravado para quem
    /// tem <c>precificacao.ler</c>: para os demais o campo é ignorado (quem não vê o custo não o altera).
    /// </summary>
    public decimal? Custo { get; set; }
}
