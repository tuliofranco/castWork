using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Projetos.Queries;

/// <summary>
/// Preços do projeto: a cópia dos do ateliê feita em <see cref="HerdadosEm"/>, talvez editada depois.
/// </summary>
public sealed class PrecosDoProjetoDto
{
    public long ProjetoId { get; init; }

    /// <summary>
    /// Só os metais com cotação no projeto, pelo nome do metal (Ouro, Prata).
    /// </summary>
    public IReadOnlyList<CotacaoDoProjetoDto> Cotacoes { get; init; } = [];

    public ParametrosDePrecoDto Parametros { get; init; } = new();

    public DateTimeOffset HerdadosEm { get; init; }

    /// <summary>
    /// Quando o projeto editou os preços; nulo enquanto estão como herdados.
    /// </summary>
    public DateTimeOffset? AlteradosEm { get; init; }
}

public sealed class CotacaoDoProjetoDto
{
    public Metal Metal { get; init; }

    /// <summary>
    /// R$/g do metal puro.
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Data de origem do valor: o registro no ateliê (herdada) ou a edição no projeto.
    /// </summary>
    public DateTimeOffset CotadaEm { get; init; }
}
