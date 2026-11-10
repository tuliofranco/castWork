using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao.Commands;

/// <summary>
/// Campos de uma liga, comuns a criar e alterar.
/// </summary>
public abstract class DadosDaLiga
{
    public string Nome { get; set; } = string.Empty;

    public Metal Metal { get; set; }

    /// <summary>
    /// Teor: "18k" ou "750".
    /// </summary>
    public string Teor { get; set; } = string.Empty;

    /// <summary>
    /// g/cm³.
    /// </summary>
    public decimal Densidade { get; set; }
}
