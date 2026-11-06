namespace Castwork.Application.Abstractions;

/// <summary>
/// Monta o documento do briefing e devolve o PDF. O layout é do sistema; a IA só fornece o conteúdo.
/// </summary>
public interface IGeradorDocumentoBriefing
{
    Task<byte[]> GerarPdfAsync(DadosDocumentoBriefing dados, CancellationToken cancellationToken);
}

/// <summary>
/// Tudo o que vai no documento, já formatado para leitura (datas, valores).
/// </summary>
public sealed record DadosDocumentoBriefing(
    MarcaDoDocumento Marca,
    long Numero,
    string Titulo,
    string? Cliente,
    DateOnly Data,
    string OQueSeraFeito,
    IReadOnlyList<(string Rotulo, string Valor)> Especificacoes,
    string? Prazo,
    string? Investimento,
    string? Observacoes,
    string? Aviso);

/// <summary>
/// Identidade do ateliê no documento: cabeçalho e rodapé.
/// </summary>
public sealed record MarcaDoDocumento(
    string Nome,
    string? Assinatura,
    string Cor,
    byte[]? Logo,
    string? Cidade,
    IReadOnlyList<string> Contatos);
