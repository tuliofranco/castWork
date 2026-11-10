using System.Text;

using Castwork.Application.Abstractions;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Gerador das imagens guardadas sem CADCraft nem SolidCraft: no 2D, um SVG com o conteúdo do arquivo e um PNG; no 3D, um
/// PNG por vista. Os PNG são só a assinatura seguida da vista e do conteúdo (o bastante para o tipo ser detectado e o teste
/// conferir de qual arquivo veio). <see cref="Falhar"/> faz o próximo pedido lançar, como o programa que cai.
/// </summary>
public sealed class GeradorDeImagensDeTeste : IGeradorDeImagens
{
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly string[] VistasDo3d = ["iso", "frente", "topo", "lado"];

    private int _falhas;
    private int _chamadas;

    public int Chamadas => _chamadas;

    public static byte[] Png(string vista, byte[] arquivo) => [.. AssinaturaPng, .. Encoding.UTF8.GetBytes($"{vista}:"), .. arquivo];

    public void Falhar() => Interlocked.Increment(ref _falhas);

    public void Reiniciar()
    {
        _falhas = 0;
        _chamadas = 0;
    }

    public Task<IReadOnlyList<ImagemGerada>> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _chamadas);
        if (Interlocked.Exchange(ref _falhas, 0) > 0)
        {
            throw new InvalidOperationException("O programa da ferramenta caiu (teste).");
        }

        IReadOnlyList<ImagemGerada> imagens = ferramenta switch
        {
            "desenho-2d" =>
            [
                new("2d", Encoding.UTF8.GetBytes($"<svg><text>{Encoding.UTF8.GetString(arquivo)}</text></svg>")),
                new("2d", Png("2d", arquivo)),
            ],
            "modelo-3d" => [.. VistasDo3d.Select(v => new ImagemGerada(v, Png(v, arquivo)))],
            _ => [],
        };
        return Task.FromResult(imagens);
    }
}
