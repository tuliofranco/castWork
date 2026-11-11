namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// Os corpos do modelo 3D com o nome da peça: o corpo único leva o nome da peça; vários ficam "Nome", "Nome (2)", "Nome (3)".
/// Na montagem da versão, cada unidade é "Nome (1)", "Nome (2)"... (<see cref="DaUnidade"/>). O SolidCraft aceita nomes de
/// até <see cref="TamanhoMaximo"/> caracteres: o nome da peça é cortado para caber o número.
/// </summary>
public static class NomesDosCorpos
{
    public const int TamanhoMaximo = 128;

    /// <summary>
    /// Os nomes de <paramref name="quantidade"/> corpos com o nome <paramref name="nome"/>, na ordem.
    /// </summary>
    public static IReadOnlyList<string> Com(string nome, int quantidade)
        => [.. Enumerable.Range(1, Math.Max(quantidade, 0)).Select(i => i == 1 ? Limitar(nome, string.Empty) : Limitar(nome, $" ({i})"))];

    /// <summary>
    /// O nome do componente da unidade <paramref name="unidade"/> da peça na montagem ("Elo oval (3)").
    /// </summary>
    public static string DaUnidade(string peca, int unidade) => Limitar(peca, $" ({unidade})");

    /// <summary>
    /// As renomeações (de, para), na ordem de aplicar, que dão aos <paramref name="corpos"/> os nomes de <see cref="Com"/>.
    /// Vazia quando os nomes já são esses. Se um nome novo ainda é de outro corpo que também muda, todos passam antes por um
    /// nome provisório, para nenhum nome repetir no meio do caminho.
    /// </summary>
    public static IReadOnlyList<(string De, string Para)> Renomeacoes(IReadOnlyList<string> corpos, string nome)
    {
        ArgumentNullException.ThrowIfNull(corpos);
        var nomes = Com(nome, corpos.Count);
        var mudancas = corpos.Select((corpo, i) => (De: corpo, Para: nomes[i])).Where(m => m.De != m.Para).ToList();
        if (!mudancas.Any(m => mudancas.Any(o => o.De == m.Para)))
        {
            return mudancas;
        }

        var provisorios = mudancas.Select((m, i) => (m.De, Para: $"castwork-{Guid.NewGuid():N}-{i}")).ToList();
        return [.. provisorios, .. provisorios.Select((p, i) => (p.Para, mudancas[i].Para))];
    }

    private static string Limitar(string nome, string sufixo)
    {
        var texto = nome.Trim();
        return (texto.Length + sufixo.Length > TamanhoMaximo ? texto[..(TamanhoMaximo - sufixo.Length)].TrimEnd() : texto) + sufixo;
    }
}
