using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// O que o joalheiro pode anexar "Do projeto" numa conversa: o arquivo atual de cada ferramenta, de todas
/// as versões, e o último PDF do briefing. Do mais recente para o mais antigo.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetAnexosDoProjetoQuery : IRequest<IReadOnlyList<AnexoDoProjetoDto>>
{
    public long ProjetoId { get; set; }
}

public sealed class GetAnexosDoProjetoQueryHandler(IUnitOfWork unitOfWork, IEnumerable<IFonteDeAnexos> fontes)
    : IRequestHandler<GetAnexosDoProjetoQuery, IReadOnlyList<AnexoDoProjetoDto>>
{
    public async Task<IReadOnlyList<AnexoDoProjetoDto>> Handle(GetAnexosDoProjetoQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.GetRepository<Projeto>().FindBy(p => p.Id == request.ProjetoId).AnyAsync(cancellationToken))
        {
            throw new EntidadeNaoEncontradaException("Projeto", request.ProjetoId);
        }

        var anexos = new List<AnexoDoProjetoDto>();
        foreach (var fonte in fontes)
        {
            anexos.AddRange(await fonte.ListarAsync(request.ProjetoId, cancellationToken));
        }

        return [.. anexos.OrderByDescending(a => a.CriadoEm)];
    }
}
