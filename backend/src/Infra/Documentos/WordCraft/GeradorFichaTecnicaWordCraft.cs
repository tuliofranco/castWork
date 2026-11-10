using Castwork.Application.Abstractions;

namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Gera o PDF da ficha técnica da peça com o WordCraft, a partir do roteiro de <see cref="RoteiroDaFichaTecnica"/>.
/// </summary>
internal sealed class GeradorFichaTecnicaWordCraft(ExecutorWordCraft executor) : IGeradorFichaTecnica
{
    public Task<byte[]> GerarPdfAsync(DadosFichaTecnica dados, CancellationToken cancellationToken)
        => executor.GerarPdfAsync(RoteiroDaFichaTecnica.Montar(dados), "Não foi possível gerar a ficha técnica da peça.", cancellationToken);
}
