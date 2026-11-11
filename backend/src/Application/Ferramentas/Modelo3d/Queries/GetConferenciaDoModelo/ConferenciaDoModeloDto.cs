using Castwork.DomainService.Precificacao;

namespace Castwork.Application.Ferramentas.Modelo3d.Queries.GetConferenciaDoModelo;

/// <summary>
/// Conferência de peso e materiais do modelo 3D da versão: os corpos com material, volume e peso, as sobreposições e os
/// materiais que se pode escolher. <see cref="ArquivoId"/> nulo: a versão não tem 3D; <see cref="Medido"/> falso: não foi
/// possível medir.
/// </summary>
public sealed class ConferenciaDoModeloDto
{
    public long VersaoId { get; init; }

    /// <summary>
    /// O arquivo 3D conferido; vai nos commands para recusar a alteração se o modelo mudou no meio do caminho.
    /// </summary>
    public long? ArquivoId { get; init; }

    public bool Medido { get; init; }

    /// <summary>
    /// A versão aceita alterar o modelo (em desenvolvimento). Publicada ou arquivada: só leitura.
    /// </summary>
    public bool Alteravel { get; init; }

    /// <summary>
    /// Peso do metal da peça (g): a soma da massa dos corpos de metal, arredondada ao mg (como no preço).
    /// </summary>
    public decimal PesoDoMetal { get; init; }

    public IReadOnlyList<PesoDaLigaDto> PesosPorLiga { get; init; } = [];

    public IReadOnlyList<CorpoConferidoDto> Corpos { get; init; } = [];

    public IReadOnlyList<SobreposicaoConferidaDto> Sobreposicoes { get; init; } = [];

    /// <summary>
    /// Os materiais que se pode escolher: as ligas ativas, "Pedra" e "Apoio (não conta)".
    /// </summary>
    public IReadOnlyList<MaterialDisponivelDto> Materiais { get; init; } = [];
}

public sealed class PesoDaLigaDto
{
    public long LigaId { get; init; }

    public string Liga { get; init; } = string.Empty;

    /// <summary>
    /// Gramas.
    /// </summary>
    public decimal Peso { get; init; }
}

public sealed class CorpoConferidoDto
{
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// O material no arquivo (<c>Castwork liga 2</c>, "Pedra", "Default"…); nulo sem nenhum.
    /// </summary>
    public string? Material { get; init; }

    public UsoDoCorpo Uso { get; init; }

    public long? LigaId { get; init; }

    /// <summary>
    /// O nome da liga (corpos de metal).
    /// </summary>
    public string? Liga { get; init; }

    /// <summary>
    /// mm³; nulo em corpo aberto.
    /// </summary>
    public decimal? VolumeMm3 { get; init; }

    /// <summary>
    /// Gramas, pela densidade do material; nulo em corpo aberto.
    /// </summary>
    public decimal? MassaGramas { get; init; }

    public bool Fechado { get; init; }

    /// <summary>
    /// Entra no peso do metal: corpo fechado de uma liga.
    /// </summary>
    public bool ContaNoPeso { get; init; }
}

public sealed class SobreposicaoConferidaDto
{
    public string CorpoA { get; init; } = string.Empty;

    public string CorpoB { get; init; } = string.Empty;

    /// <summary>
    /// Volume em comum (mm³).
    /// </summary>
    public decimal VolumeMm3 { get; init; }

    /// <summary>
    /// Os dois são de metal: a parte comum conta duas vezes no peso.
    /// </summary>
    public bool DeMetal { get; init; }

    /// <summary>
    /// Os dois são da mesma liga e podem virar um corpo só.
    /// </summary>
    public bool PodeUnir { get; init; }
}

public sealed class MaterialDisponivelDto
{
    /// <summary>
    /// O nome no arquivo, que vai no command.
    /// </summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// Como aparece na tela ("Ouro 18k amarelo", "Pedra").
    /// </summary>
    public string Rotulo { get; init; } = string.Empty;

    public UsoDoCorpo Uso { get; init; }

    public long? LigaId { get; init; }
}
