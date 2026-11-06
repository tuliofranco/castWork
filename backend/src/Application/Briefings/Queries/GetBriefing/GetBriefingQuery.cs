using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Briefings;

using MediatR;

namespace Castwork.Application.Briefings.Queries.GetBriefing;

/// <summary>
/// O briefing do projeto com a conversa e os PDFs gerados, ou nulo se o formulário ainda não foi preenchido.
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
            .Include(b => b.Mensagens).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (briefing is null)
        {
            return null;
        }

        var documentos = await unitOfWork.GetRepository<DocumentoBriefing>().FindBy(d => d.BriefingId == briefing.Id)
            .OrderByDescending(d => d.GeradoEm)
            .Select(d => new DocumentoBriefingDto(d.Id, d.NomeArquivo, d.GeradoEm))
            .AsNoTracking().ToListAsync(cancellationToken);

        var mensagens = briefing.Mensagens
            .Where(m => m.Papel != PapelMensagem.Ferramenta && (m.Texto is not null || m.Imagens.Count > 0))
            .OrderBy(m => m.Ordem)
            .Select(m => new MensagemBriefingDto(m.Ordem, m.Papel, m.Texto, m.Imagens, m.CriadaEm))
            .ToList();

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
            mensagens,
            documentos);
    }
}
