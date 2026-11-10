namespace Castwork.Application.Abstractions;

/// <summary>
/// Gera a prévia (SVG) do arquivo de uma ferramenta, para mostrar o desenho fora do editor.
/// </summary>
public interface IPreviaFerramenta
{
    /// <summary>
    /// SVG do arquivo (ou PNG, na ferramenta sem exportação vetorial, como o 3D), ou nulo se a ferramenta não tem prévia ou o programa dela não está disponível.
    /// </summary>
    Task<byte[]?> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken);
}

public static class TipoDaPrevia
{
    private static readonly byte[] AssinaturaPng = [0x89, 0x50, 0x4E, 0x47];

    /// <summary>O tipo da prévia pelo conteúdo: PNG pela assinatura, senão SVG.</summary>
    public static string De(byte[] previa) => previa.AsSpan().StartsWith(AssinaturaPng) ? "image/png" : "image/svg+xml";
}
