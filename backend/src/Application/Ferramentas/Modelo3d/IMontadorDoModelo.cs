namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// "Montar a partir das peças": traz para o arquivo 3D da versão uma cópia do modelo 3D de cada peça da composição, uma por
/// unidade, enfileiradas ao lado do que o modelo já tem (para a IA ou o joalheiro posicionarem depois). Cada unidade é um
/// componente com o nome da peça e o número dela ("Elo oval (1)"), e os corpos dela levam o mesmo nome. Os corpos mantêm o
/// material do arquivo da peça; os sem material recebem o da liga da peça. Sem IA.
/// </summary>
public interface IMontadorDoModelo
{
    /// <summary>
    /// O arquivo da versão montado. <paramref name="arquivoDaVersao"/> nulo: começa um modelo novo. SolidCraft
    /// indisponível: <c>ServicoIndisponivelException</c>; modelo de peça que não abre ou montagem recusada:
    /// <c>RegraDeNegocioException</c>.
    /// </summary>
    Task<byte[]> MontarAsync(byte[]? arquivoDaVersao, IReadOnlyList<MaterialDoModelo> materiais, IReadOnlyList<PecaParaMontar> pecas, CancellationToken cancellationToken);
}

/// <summary>
/// Uma peça da composição com o modelo 3D dela (o arquivo final), quantas unidades entram e o material da liga dela no
/// arquivo (<c>Castwork liga {id}</c>; nulo sem liga ativa).
/// </summary>
public sealed record PecaParaMontar(string Nome, byte[] Arquivo, int Quantidade, string? MaterialDaLiga);
