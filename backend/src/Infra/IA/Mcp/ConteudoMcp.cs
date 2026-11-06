namespace Castwork.Infra.IA.Mcp;

/// <summary>
/// Um bloco do retorno: texto ou imagem (bytes e tipo MIME).
/// </summary>
internal sealed record ConteudoMcp(string? Texto, byte[]? Imagem, string? TipoDaImagem);
