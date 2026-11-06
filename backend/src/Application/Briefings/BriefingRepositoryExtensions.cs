using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.Application.Briefings;

internal static class BriefingRepositoryExtensions
{
    /// <summary>
    /// O briefing do projeto, rastreado e com a conversa, ou 404 se o formulário ainda não foi preenchido.
    /// </summary>
    public static async Task<Briefing> ObterDoProjetoAsync(this IUnitOfWork unitOfWork, long projetoId, CancellationToken cancellationToken)
    {
        var repositorio = unitOfWork.GetRepository<Briefing>();
        var id = await repositorio.FindBy(b => b.ProjetoId == projetoId).Select(b => (long?)b.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Briefing do projeto", projetoId);
        return await repositorio.ObterPorIdOuFalharAsync(id, cancellationToken);
    }
}
