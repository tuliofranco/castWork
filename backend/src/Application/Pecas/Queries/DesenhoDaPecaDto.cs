namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// Um desenho da peça da biblioteca (sem o conteúdo, que vem pela rota REST <c>api/pecas/{id}/desenhos/{ferramenta}</c>):
/// a ferramenta, quando foi copiado do projeto (ao publicar) e o tamanho em bytes.
/// </summary>
public sealed class DesenhoDaPecaDto
{
    public long PecaId { get; init; }

    /// <summary>
    /// <c>desenho-2d</c> ou <c>modelo-3d</c>.
    /// </summary>
    public string Ferramenta { get; init; } = string.Empty;

    public DateTimeOffset CopiadoEm { get; init; }

    public int Tamanho { get; init; }
}
