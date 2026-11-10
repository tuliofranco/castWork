using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// O conteúdo de um desenho da peça da biblioteca (o DXF do desenho 2D ou o JSON do modelo 3D), só para ver. Nulo quando
/// a peça não tem desenho naquela ferramenta.
/// </summary>
[Permissao(Regras.Pecas.Consultar)]
public sealed class GetDesenhoDaPecaQuery : IRequest<byte[]?>
{
    public long PecaId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetDesenhoDaPecaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetDesenhoDaPecaQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetDesenhoDaPecaQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.GetRepository<Peca>().ExisteAsync(request.PecaId, cancellationToken))
        {
            throw new Domain.Exceptions.EntidadeNaoEncontradaException("Peça", request.PecaId);
        }

        return await unitOfWork.GetRepository<Peca>()
            .FindBy(p => p.Id == request.PecaId)
            .SelectMany(p => p.Desenhos)
            .Where(d => d.Ferramenta == request.Ferramenta)
            .Select(d => d.Conteudo)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
