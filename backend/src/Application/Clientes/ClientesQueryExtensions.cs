using Castwork.Domain.Entities.Clientes;

namespace Castwork.Application.Clientes;

public static class ClientesQueryExtensions
{
    /// <summary>
    /// Busca por trecho do nome, sem diferenciar maiúsculas (vira <c>lower(nome) LIKE</c> no PostgreSQL).
    /// </summary>
    public static IQueryable<Cliente> ComNome(this IQueryable<Cliente> query, string? termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return query;
        }

#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var termoMinusculo = termo.Trim().ToLower();
        return query.Where(c => c.Nome.ToLower().Contains(termoMinusculo));
#pragma warning restore CA1304, CA1311, CA1862
    }
}
