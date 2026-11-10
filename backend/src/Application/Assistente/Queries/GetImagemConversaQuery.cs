using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// Uma imagem da conversa da ferramenta (anexada ou gerada pela IA). Precisa ser do escopo informado (versão ou peça) ou do
/// projeto.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetImagemConversaQuery : IRequest<ImagemDaFerramentaDto>, IEscopoDaFerramenta
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public Guid Chave { get; set; }
}

public sealed class GetImagemConversaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagemConversaQuery, ImagemDaFerramentaDto>
{
    public async Task<ImagemDaFerramentaDto> Handle(GetImagemConversaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);

        // As do escopo da rota e as do projeto (o briefing aparece em toda versão e em toda peça).
        var (versao, peca) = (ConversaDoEscopo.VersaoDaRota(request.VersaoId), ConversaDoEscopo.PecaDaRota(request.PecaDoProjetoId));
        var conversas = unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.ProjetoId == request.ProjetoId
                && ((c.VersaoId == null && c.PecaDoProjetoId == null) || (versao != null && c.VersaoId == versao) || (peca != null && c.PecaDoProjetoId == peca)))
            .Select(c => c.Id);
        return await unitOfWork.GetRepository<ImagemConversa>()
            .FindBy(i => i.Chave == request.Chave && conversas.Contains(i.ConversaId))
            .Select(i => new ImagemDaFerramentaDto(i.Tipo, i.Conteudo))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Imagem", request.Chave);
    }
}
