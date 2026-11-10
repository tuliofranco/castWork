using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// A prévia (SVG) de um desenho da peça da biblioteca, para o visualizador só de ver. Nula quando a peça não tem desenho
/// naquela ferramenta ou o programa da ferramenta não está disponível.
/// </summary>
[Permissao(Regras.Pecas.Consultar)]
public sealed class GetPreviaDoDesenhoDaPecaQuery : IRequest<byte[]?>
{
    public long PecaId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetPreviaDoDesenhoDaPecaQueryHandler(IUnitOfWork unitOfWork, IPreviaFerramenta previa)
    : IRequestHandler<GetPreviaDoDesenhoDaPecaQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetPreviaDoDesenhoDaPecaQuery request, CancellationToken cancellationToken)
    {
        var desenho = await new GetDesenhoDaPecaQueryHandler(unitOfWork)
            .Handle(new GetDesenhoDaPecaQuery { PecaId = request.PecaId, Ferramenta = request.Ferramenta }, cancellationToken);
        return desenho is null ? null : await previa.GerarAsync(request.Ferramenta, desenho, cancellationToken);
    }
}
