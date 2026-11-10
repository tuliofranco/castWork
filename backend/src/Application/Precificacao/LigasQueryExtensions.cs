using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao;

public static class LigasQueryExtensions
{
    /// <summary>
    /// As ligas ativas por metal, do teor maior para o menor e por nome. É a lista dos preços do ateliê e a do seletor de
    /// liga das peças.
    /// </summary>
    public static IQueryable<LigaDto> AtivasParaDto(this IQueryable<Liga> query)
        => query
            .Where(l => l.Ativo)
            .OrderBy(l => l.Metal).ThenByDescending(l => l.Teor).ThenBy(l => l.Nome)
            .Select(l => new LigaDto { Id = l.Id, Nome = l.Nome, Metal = l.Metal, Teor = l.Teor.Milesimos, Densidade = l.Densidade });
}
