using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// As imagens guardadas do desenho da peça da biblioteca numa ferramenta (vista e tipo, sem o conteúdo), na ordem de
/// mostrar. Vazia se a peça não tem o desenho ou ele foi publicado antes de as imagens existirem.
/// </summary>
[Permissao(Regras.Pecas.Consultar)]
public sealed class GetImagensDoDesenhoDaPecaQuery : IRequest<IReadOnlyList<ImagemGuardadaDto>>
{
    public long PecaId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetImagensDoDesenhoDaPecaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagensDoDesenhoDaPecaQuery, IReadOnlyList<ImagemGuardadaDto>>
{
    public async Task<IReadOnlyList<ImagemGuardadaDto>> Handle(GetImagensDoDesenhoDaPecaQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.GetRepository<Peca>().ExisteAsync(request.PecaId, cancellationToken))
        {
            throw new EntidadeNaoEncontradaException("Peça", request.PecaId);
        }

        var imagens = await unitOfWork.GetRepository<Peca>()
            .FindBy(p => p.Id == request.PecaId)
            .SelectMany(p => p.Desenhos)
            .Where(d => d.Ferramenta == request.Ferramenta)
            .SelectMany(d => d.Imagens)
            .Select(i => new ImagemGuardadaDto(i.Vista, i.Tipo))
            .ToListAsync(cancellationToken);
        return ImagensGuardadas.Ordenar(imagens, i => i.Vista, i => i.Tipo);
    }
}
