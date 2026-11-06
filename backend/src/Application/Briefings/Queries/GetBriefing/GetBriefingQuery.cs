using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Briefings;

using MediatR;

namespace Castwork.Application.Briefings.Queries.GetBriefing;

/// <summary>
/// O briefing do projeto com os PDFs gerados (a conversa é a da ferramenta "briefing"), ou nulo se o formulário ainda não foi preenchido.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetBriefingQuery : IRequest<BriefingDto?>
{
    public long ProjetoId { get; set; }
}

public sealed class GetBriefingQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetBriefingQuery, BriefingDto?>
{
    public async Task<BriefingDto?> Handle(GetBriefingQuery request, CancellationToken cancellationToken)
    {
        var briefing = await unitOfWork.GetRepository<Briefing>().FindBy(b => b.ProjetoId == request.ProjetoId)
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (briefing is null)
        {
            return null;
        }

        var documentos = await unitOfWork.GetRepository<DocumentoBriefing>().FindBy(d => d.BriefingId == briefing.Id)
            .OrderByDescending(d => d.GeradoEm)
            .Select(d => new DocumentoBriefingDto(d.Id, d.NomeArquivo, d.GeradoEm))
            .AsNoTracking().ToListAsync(cancellationToken);

        return new BriefingDto(
            briefing.Id,
            briefing.ProjetoId,
            briefing.TipoJoia,
            briefing.OQueOClienteQuer,
            briefing.Ocasiao,
            briefing.Materiais,
            briefing.Medidas,
            briefing.Prazo,
            briefing.Orcamento,
            briefing.OQueSeraFeito,
            briefing.Acabamento,
            briefing.Pedras,
            briefing.Fecho,
            briefing.Observacoes,
            documentos);
    }
}
