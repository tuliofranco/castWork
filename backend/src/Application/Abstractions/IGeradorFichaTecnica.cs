namespace Castwork.Application.Abstractions;

/// <summary>
/// Monta a ficha técnica da peça do projeto e devolve o PDF, com o mesmo visual do briefing (marca do ateliê no topo e
/// seções). O layout é do sistema; o handler só fornece o conteúdo.
/// </summary>
public interface IGeradorFichaTecnica
{
    Task<byte[]> GerarPdfAsync(DadosFichaTecnica dados, CancellationToken cancellationToken);
}

/// <summary>
/// Tudo o que vai na ficha, já formatado para leitura. <see cref="Valor"/> só vem para quem pode ver preços; nulo, a
/// linha some do documento. <see cref="Briefing"/> é o resumo do briefing do projeto, quando existe. <see cref="Imagens"/>: as
/// imagens guardadas do desenho da peça (a iso do modelo 3D e a do desenho 2D, em PNG), quando existem.
/// </summary>
public sealed record DadosFichaTecnica(
    MarcaDoDocumento Marca,
    string Nome,
    string Projeto,
    string? Cliente,
    DateOnly Data,
    IReadOnlyList<(string Rotulo, string Valor)> Especificacoes,
    string? Descricao,
    string? Valor,
    string? Briefing,
    IReadOnlyList<ImagemDaFicha>? Imagens = null);

/// <summary>
/// Uma imagem da ficha (PNG) com a legenda.
/// </summary>
public sealed record ImagemDaFicha(string Legenda, byte[] Png);
