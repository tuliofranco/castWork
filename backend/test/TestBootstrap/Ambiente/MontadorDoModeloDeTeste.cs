using System.Text;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Montador de mentira (sem SolidCraft): devolve o arquivo da versão (ou um em branco) com uma linha por peça montada
/// ("Elo oval × 3") e guarda as chamadas. <see cref="Falha"/>: a exceção que ele lança no lugar. Zerado a cada teste.
/// </summary>
public sealed class MontadorDoModeloDeTeste : IMontadorDoModelo
{
    private readonly List<(byte[]? ArquivoDaVersao, IReadOnlyList<PecaParaMontar> Pecas)> _chamadas = [];

    public Exception? Falha { get; set; }

    public IReadOnlyList<(byte[]? ArquivoDaVersao, IReadOnlyList<PecaParaMontar> Pecas)> Chamadas => _chamadas;

    public Task<byte[]> MontarAsync(byte[]? arquivoDaVersao, IReadOnlyList<MaterialDoModelo> materiais, IReadOnlyList<PecaParaMontar> pecas, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pecas);
        lock (_chamadas)
        {
            _chamadas.Add((arquivoDaVersao, pecas));
        }

        if (Falha is not null)
        {
            throw Falha;
        }

        var montagem = string.Concat(pecas.Select(p => $"\n{p.Nome} × {p.Quantidade}"));
        return Task.FromResult<byte[]>([.. arquivoDaVersao ?? "{}"u8.ToArray(), .. Encoding.UTF8.GetBytes(montagem)]);
    }

    public void Reiniciar()
    {
        Falha = null;
        lock (_chamadas)
        {
            _chamadas.Clear();
        }
    }
}
