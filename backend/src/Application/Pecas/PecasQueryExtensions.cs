using Castwork.Application.Pecas.Queries;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Pecas;

public static class PecasQueryExtensions
{
    /// <summary>
    /// Busca por trecho do nome ou do material, sem diferenciar maiúsculas.
    /// </summary>
    public static IQueryable<Peca> ComTermo(this IQueryable<Peca> query, string? termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return query;
        }

#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var termoMinusculo = termo.Trim().ToLower();
        return query.Where(p => p.Nome.ToLower().Contains(termoMinusculo)
            || (p.Material != null && p.Material.ToLower().Contains(termoMinusculo)));
#pragma warning restore CA1304, CA1311, CA1862
    }

    /// <summary>
    /// Histórico de uso das peças da biblioteca: cada peça de projeto vinculada a uma delas, com o projeto, o valor e as
    /// versões que a usam. A peça que teve os dados alterados perdeu o vínculo e não aparece (virou outra peça).
    /// </summary>
    public static IQueryable<UsoDaPecaDto> ParaHistoricoDeUso(this IQueryable<Projeto> projetos, IReadOnlyCollection<long> pecaIds)
        => projetos.SelectMany(p => p.Pecas
            .Where(pp => pp.PecaId != null && pecaIds.Contains(pp.PecaId.Value))
            .Select(pp => new UsoDaPecaDto
            {
                PecaId = pp.PecaId!.Value,
                ProjetoId = p.Id,
                Projeto = p.Nome,
                PecaDoProjetoId = pp.Id,
                Valor = pp.Valor == null ? null : pp.Valor.Valor,
                Versoes = p.Versoes
                    .SelectMany(v => v.Itens
                        .Where(i => i.PecaDoProjetoId == pp.Id)
                        .Select(i => new VersaoQueUsaPecaDto { VersaoId = v.Id, Numero = v.Numero, Nome = v.Nome, Status = v.Status, Quantidade = i.Quantidade }))
                    .OrderBy(v => v.Numero)
                    .ToList(),
            }));
}
