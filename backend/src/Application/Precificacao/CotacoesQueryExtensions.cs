using Castwork.Application.Precificacao.Queries;
using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao;

public static class CotacoesQueryExtensions
{
    /// <summary>
    /// A vigente primeiro. O Id desempata registros no mesmo instante.
    /// </summary>
    public static IOrderedQueryable<CotacaoMetal> MaisRecentesPrimeiro(this IQueryable<CotacaoMetal> query)
        => query.OrderByDescending(c => c.RegistradaEm).ThenByDescending(c => c.Id);

    /// <summary>
    /// Projeção com o nome do autor (subconsulta em <c>usuarios</c>, sem FK: a cotação sobrevive ao usuário).
    /// Ordenar antes: o valor sai do value object na projeção final.
    /// </summary>
    public static IQueryable<CotacaoDto> ParaDto(this IQueryable<CotacaoMetal> query, IQueryable<Usuario> usuarios)
        => query.Select(c => new CotacaoDto
        {
            Id = c.Id,
            Metal = c.Metal,
            Valor = c.Valor.Valor,
            RegistradaEm = c.RegistradaEm,
            RegistradaPor = usuarios.Where(u => u.Id == c.RegistradaPor).Select(u => u.Nome).FirstOrDefault(),
        });
}
