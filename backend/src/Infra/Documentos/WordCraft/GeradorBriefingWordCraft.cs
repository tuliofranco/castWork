using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Gera o PDF do briefing com o WordCraft, a partir do roteiro de <see cref="RoteiroDoBriefing"/>.
/// </summary>
internal sealed class GeradorBriefingWordCraft(ExecutorWordCraft executor) : IGeradorDocumentoBriefing
{
    public Task<byte[]> GerarPdfAsync(DadosDocumentoBriefing dados, CancellationToken cancellationToken)
        => executor.GerarPdfAsync(RoteiroDoBriefing.Montar(dados), "Não foi possível gerar o PDF do briefing.", cancellationToken);
}
