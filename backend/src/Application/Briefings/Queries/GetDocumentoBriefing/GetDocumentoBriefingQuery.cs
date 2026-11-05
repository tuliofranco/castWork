using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Briefings.Queries.GetDocumentoBriefing;

/// <summary>
/// O arquivo de um PDF gerado, para download. O documento precisa ser do briefing do projeto informado.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetDocumentoBriefingQuery : IRequest<ArquivoDto>
{
    public long ProjetoId { get; set; }

    public long DocumentoId { get; set; }
}

public sealed record ArquivoDto(string NomeArquivo, byte[] Conteudo);

public sealed class GetDocumentoBriefingQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetDocumentoBriefingQuery, ArquivoDto>
{
    public async Task<ArquivoDto> Handle(GetDocumentoBriefingQuery request, CancellationToken cancellationToken)
    {
        var briefings = unitOfWork.GetRepository<Briefing>().FindBy(b => b.ProjetoId == request.ProjetoId).Select(b => b.Id);
        return await unitOfWork.GetRepository<DocumentoBriefing>()
            .FindBy(d => d.Id == request.DocumentoId && briefings.Contains(d.BriefingId))
            .Select(d => new ArquivoDto(d.NomeArquivo, d.Conteudo))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Documento", request.DocumentoId);
    }
}
