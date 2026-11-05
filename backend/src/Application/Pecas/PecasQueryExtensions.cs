using Castwork.Domain.Entities.Pecas;

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
}
