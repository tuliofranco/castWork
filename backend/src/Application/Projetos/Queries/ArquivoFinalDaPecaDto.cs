namespace Castwork.Application.Projetos.Queries;

/// <summary>
/// O arquivo final de uma ferramenta da peça do projeto (sem o conteúdo, que vem pela rota REST da ferramenta com
/// <c>?final=true</c>): a ferramenta, o Id do arquivo, o nome dado pelo joalheiro (nulo: o padrão da ferramenta) e quando
/// foi gravado. A ferramenta sem final não aparece.
/// </summary>
public sealed class ArquivoFinalDaPecaDto
{
    public long PecaDoProjetoId { get; init; }

    /// <summary>
    /// <c>desenho-2d</c> ou <c>modelo-3d</c>.
    /// </summary>
    public string Ferramenta { get; init; } = string.Empty;

    public long ArquivoId { get; init; }

    public string? Nome { get; init; }

    public DateTimeOffset CriadoEm { get; init; }
}
