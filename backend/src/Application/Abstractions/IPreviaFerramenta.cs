namespace Castwork.Application.Abstractions;

/// <summary>
/// Gera a prévia (SVG) do arquivo de uma ferramenta, para mostrar o desenho fora do editor.
/// </summary>
public interface IPreviaFerramenta
{
    /// <summary>
    /// SVG do arquivo, ou nulo se a ferramenta não tem prévia ou o programa dela não está disponível.
    /// </summary>
    Task<byte[]?> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken);
}
