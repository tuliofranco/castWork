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
    IReadOnlyList<MensagemBriefingDto> Mensagens,
    IReadOnlyList<DocumentoBriefingDto> Documentos);

/// <summary>
/// Só as mensagens visíveis no chat (o conteúdo bruto da IA e das ferramentas não sai da API).
/// <see cref="Imagens"/> são as chaves das imagens anexadas, lidas em <c>briefing/imagens/{chave}</c>.
/// </summary>
public sealed record MensagemBriefingDto(int Ordem, PapelMensagem Papel, string? Texto, IReadOnlyList<Guid> Imagens, DateTimeOffset CriadaEm);

public sealed record DocumentoBriefingDto(long Id, string NomeArquivo, DateTimeOffset GeradoEm);
