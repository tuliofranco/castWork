namespace Castwork.Application.Abstractions;

/// <summary>
/// Gera as imagens em alta qualidade do arquivo de uma ferramenta, pelo programa dela e sem IA: as vistas do modelo 3D
/// (iso, frente, topo, lado) e o desenho 2D (SVG e PNG). Lista vazia se a ferramenta não guarda imagens ou o programa
/// dela não está disponível; lança se o programa falhar.
/// </summary>
public interface IGeradorDeImagens
{
    Task<IReadOnlyList<ImagemGerada>> GerarAsync(string ferramenta, byte[] arquivo, CancellationToken cancellationToken);
}

/// <summary>
/// Uma imagem gerada: a vista (<c>Domain.Entities.Assistente.VistaDaImagem</c>) e o conteúdo (PNG ou SVG).
/// </summary>
public sealed record ImagemGerada(string Vista, byte[] Conteudo);
