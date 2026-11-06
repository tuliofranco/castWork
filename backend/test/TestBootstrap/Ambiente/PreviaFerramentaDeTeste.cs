using System.Text;

using Castwork.Application.Abstractions;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Prévia de mentira (sem CADCraft): um SVG com o conteúdo do arquivo num texto.
/// </summary>
public sealed class PreviaFerramentaDeTeste : IPreviaFerramenta
{
    public Task<byte[]?> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken)
        => Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes($"<svg><text>{Encoding.UTF8.GetString(arquivo)}</text></svg>"));
}
