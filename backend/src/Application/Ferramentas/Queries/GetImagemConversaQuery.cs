using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// Uma imagem da conversa da ferramenta (anexada ou gerada pela IA). Precisa ser da versão informada.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetImagemConversaQuery : IRequest<ImagemDaFerramentaDto>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public Guid Chave { get; set; }
}

public sealed class GetImagemConversaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagemConversaQuery, ImagemDaFerramentaDto>
{
    public async Task<ImagemDaFerramentaDto> Handle(GetImagemConversaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversas = unitOfWork.GetRepository<ConversaFerramenta>().FindBy(c => c.ProjetoId == request.ProjetoId && (c.VersaoId == null || c.VersaoId == request.VersaoId)).Select(c => c.Id);
        return await unitOfWork.GetRepository<ImagemConversa>()
            .FindBy(i => i.Chave == request.Chave && conversas.Contains(i.ConversaId))
            .Select(i => new ImagemDaFerramentaDto(i.Tipo, i.Conteudo))
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Imagem", request.Chave);
    }
}
