using Castwork.Domain.ValueObjects;

namespace Castwork.DomainService.Precificacao;

/// <summary>
/// O modelo 3D atual da versão, medido sem IA: cada corpo já classificado pelo material (liga, pedra, apoio ou sem
/// material) e as sobreposições entre corpos. <see cref="Medido"/> falso: a versão tem 3D, mas não foi possível medir.
/// Na <see cref="EntradaDoPreco"/>, nulo é versão sem 3D (vale o peso manual).
/// </summary>
public sealed record ModeloMedido(bool Medido, IReadOnlyList<CorpoDoModelo> Corpos, IReadOnlyList<SobreposicaoDeCorpos> Sobreposicoes)
{
    public static ModeloMedido NaoMedido { get; } = new(Medido: false, [], []);

    /// <summary>
    /// Peso da peça por liga: a soma da massa dos corpos de metal (a massa vem do volume × densidade do material). Pedra,
    /// apoio e corpo sem material ficam fora; corpo aberto não tem massa e também fica fora. Soma a massa medida e só então
    /// arredonda ao miligrama. Na ordem da liga.
    /// </summary>
    public IReadOnlyList<(long LigaId, Peso Peso)> PesoPorLiga()
        => [.. Corpos
            .Where(c => c is { Uso: UsoDoCorpo.Metal, LigaId: not null, MassaGramas: not null })
            .GroupBy(c => c.LigaId!.Value)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, Peso.EmGramas(g.Sum(c => c.MassaGramas!.Value))))];

    /// <summary>
    /// Corpos de metal sobrepostos: a parte comum seria contada duas vezes no peso.
    /// </summary>
    public IReadOnlyList<SobreposicaoDeCorpos> SobreposicoesDeMetal()
    {
        var metais = Corpos.Where(c => c.Uso == UsoDoCorpo.Metal).Select(c => c.Nome).ToHashSet(StringComparer.Ordinal);
        return [.. Sobreposicoes.Where(s => metais.Contains(s.CorpoA) && metais.Contains(s.CorpoB))];
    }
}

/// <summary>
/// Um corpo do modelo: o nome, para que serve (pelo material), a liga quando é metal, a massa medida em gramas, sem
/// arredondar (nula em corpo aberto: o SolidCraft não mede volume de corpo aberto) e se é fechado.
/// </summary>
public sealed record CorpoDoModelo(string Nome, UsoDoCorpo Uso, long? LigaId, decimal? MassaGramas, bool Fechado);

/// <summary>
/// Dois corpos que se sobrepõem e o volume em comum (mm³).
/// </summary>
public sealed record SobreposicaoDeCorpos(string CorpoA, string CorpoB, decimal VolumeMm3);
