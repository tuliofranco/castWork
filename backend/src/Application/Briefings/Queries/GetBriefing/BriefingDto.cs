using Castwork.Domain.Entities.Briefings;

namespace Castwork.Application.Briefings.Queries.GetBriefing;

public sealed record BriefingDto(
    long Id,
    long ProjetoId,
    TipoJoia TipoJoia,
    string OQueOClienteQuer,
    string? Ocasiao,
    string? Materiais,
    string? Medidas,
    DateOnly? Prazo,
    string? Orcamento,
    string? OQueSeraFeito,
    string? Acabamento,
    string? Pedras,
    string? Fecho,
    string? Observacoes,
    IReadOnlyList<DocumentoBriefingDto> Documentos);

public sealed record DocumentoBriefingDto(long Id, string NomeArquivo, DateTimeOffset GeradoEm);
