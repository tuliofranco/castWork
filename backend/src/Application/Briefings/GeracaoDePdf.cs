using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Briefings;

namespace Castwork.Application.Briefings;

/// <summary>
/// Gera o PDF do briefing e o guarda como novo documento. Usado pelo botão e pela ferramenta da IA.
/// O documento entra na mesma unidade de trabalho do command que chamou.
/// </summary>
internal static class GeracaoDePdf
{
    public static async Task<DocumentoBriefing> GerarAsync(
        IUnitOfWork unitOfWork,
        IGeradorDocumentoBriefing gerador,
        TimeProvider relogio,
        Briefing briefing,
        CancellationToken cancellationToken)
    {
        // Hora local só para o que o joalheiro lê (data no PDF, nome do arquivo); o banco guarda UTC.
        var local = relogio.GetLocalNow();
        var pdf = await PdfAsync(unitOfWork, gerador, relogio, briefing, cancellationToken);
        var documento = DocumentoBriefing.Criar(briefing.Id, $"briefing-{local:yyyyMMdd-HHmm}.pdf", pdf, relogio.GetUtcNow());
        unitOfWork.GetRepository<DocumentoBriefing>().Adicionar(documento);
        return documento;
    }

    /// <summary>
    /// Só o arquivo, sem guardar: usado na pré-visualização.
    /// </summary>
    public static async Task<byte[]> PdfAsync(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio, Briefing briefing, CancellationToken cancellationToken)
    {
        var hoje = DateOnly.FromDateTime(relogio.GetLocalNow().DateTime);
        return await gerador.GerarPdfAsync(await ContextoDoBriefing.ParaDocumentoAsync(unitOfWork, briefing, hoje, cancellationToken), cancellationToken);
    }
}
