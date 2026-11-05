using Castwork.Application.Common.Mapping;
using Castwork.Domain.Entities.Pecas;

namespace Castwork.Application.Pecas.Queries.GetPecas;

/// <summary>
/// Linha da listagem de peças. Só o que a tela usa: é também a lista de campos ordenáveis.
/// </summary>
public sealed class GetPecasDto : IMapFrom<Peca>
{
    public long Id { get; init; }

    public string Titulo { get; init; } = string.Empty;

    public StatusPeca Status { get; init; }

    public long ClienteId { get; init; }

    public string ClienteNome { get; init; } = string.Empty;

    public DateOnly? PrazoEntrega { get; init; }

    public DateTimeOffset CriadoEm { get; init; }

    public DateTimeOffset? AlteradoEm { get; init; }
}
