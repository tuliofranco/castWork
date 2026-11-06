using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Ferramentas.Briefings.Queries.PreVisualizarPdfBriefing;

/// <summary>
/// Gera o PDF com os dados atuais, sem guardar: o joalheiro confere antes de salvar e baixar.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class PreVisualizarPdfBriefingQuery : IRequest<byte[]>
{
    public long ProjetoId { get; set; }
}

public sealed class PreVisualizarPdfBriefingQueryHandler(IUnitOfWork unitOfWork, IGeradorDocumentoBriefing gerador, TimeProvider relogio)
    : IRequestHandler<PreVisualizarPdfBriefingQuery, byte[]>
{
    public async Task<byte[]> Handle(PreVisualizarPdfBriefingQuery request, CancellationToken cancellationToken)
    {
        var briefing = await unitOfWork.ObterDoProjetoAsync(request.ProjetoId, cancellationToken);
        return await GeracaoDePdf.PdfAsync(unitOfWork, gerador, relogio, briefing, cancellationToken);
    }
}
