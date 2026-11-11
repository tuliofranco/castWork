using Castwork.Domain.Entities.Pecas;

namespace Castwork.Application.Pecas.Commands;

/// <summary>
/// Campos de uma peça, comuns à peça da biblioteca e à peça do projeto (criar e alterar). O material é a liga (peças de
/// metal) ou, na pedra, o texto livre.
/// </summary>
public interface IDadosDaPeca
{
    string Nome { get; }

    TipoPeca Tipo { get; }

    /// <summary>
    /// Liga do ateliê, em todo tipo menos pedra. Opcional. A peça guarda o Id e uma cópia do nome e do teor da liga.
    /// </summary>
    long? LigaId { get; }

    /// <summary>
    /// Material livre, só na pedra ("Turmalina"). Opcional.
    /// </summary>
    string? Material { get; }

    decimal? PesoGramas { get; }

    string? Descricao { get; }
}

/// <summary>
/// Base dos commands da biblioteca (criar e alterar peça).
/// </summary>
public abstract class DadosDaPeca : IDadosDaPeca
{
    public string Nome { get; set; } = string.Empty;

    public TipoPeca Tipo { get; set; }

    /// <inheritdoc/>
    public long? LigaId { get; set; }

    /// <inheritdoc/>
    public string? Material { get; set; }

    public decimal? PesoGramas { get; set; }

    public string? Descricao { get; set; }
}
