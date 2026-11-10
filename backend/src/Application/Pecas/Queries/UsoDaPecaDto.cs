using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// Um projeto que usa a peça da biblioteca, por meio da cópia vinculada a ela (importada ou publicada de lá), com o valor
/// da peça naquele projeto e as versões cuja composição a usa (pode não haver nenhuma ainda).
/// </summary>
public sealed class UsoDaPecaDto
{
    /// <summary>
    /// A peça da biblioteca.
    /// </summary>
    public long PecaId { get; init; }

    public long ProjetoId { get; init; }

    public string Projeto { get; init; } = string.Empty;

    /// <summary>
    /// A cópia da peça no projeto.
    /// </summary>
    public long PecaDoProjetoId { get; init; }

    /// <summary>
    /// Valor da peça no projeto (custo de uma unidade); nulo sem valor. É dado de precificação.
    /// </summary>
    public decimal? Valor { get; init; }

    public IReadOnlyList<VersaoQueUsaPecaDto> Versoes { get; init; } = [];
}

public sealed class VersaoQueUsaPecaDto
{
    public long VersaoId { get; init; }

    public int Numero { get; init; }

    public string Nome { get; init; } = string.Empty;

    public StatusVersao Status { get; init; }

    public int Quantidade { get; init; }
}
