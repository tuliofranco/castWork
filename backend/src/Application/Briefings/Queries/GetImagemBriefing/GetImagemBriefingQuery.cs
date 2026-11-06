using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Briefings.Queries.GetImagemBriefing;

/// <summary>
/// Uma imagem enviada na conversa do briefing. A imagem precisa ser do briefing do projeto informado.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetImagemBriefingQuery : IRequest<ImagemBriefingDto>
{
    public long ProjetoId { get; set; }

    public Guid Chave { get; set; }
}

public sealed record ImagemBriefingDto(string Tipo, byte[] Conteudo);

public sealed class GetImagemBriefingQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagemBriefingQuery, ImagemBriefingDto>
{
    public async Task<ImagemBriefingDto> Handle(GetImagemBriefingQuery request, CancellationToken cancellationToken)
    {
        var briefings = unitOfWork.GetRepository<Briefing>().FindBy(b => b.ProjetoId == request.ProjetoId).Select(b => b.Id);
        return await unitOfWork.GetRepository<ImagemBriefing>()
            .FindBy(i => i.Chave == request.Chave && briefings.Contains(i.BriefingId))
            .Select(i => new ImagemBriefingDto(i.Tipo, i.Conteudo))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Imagem", request.Chave);
    }
}
