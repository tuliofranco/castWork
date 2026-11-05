using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Projetos;

public static class ProjetosQueryExtensions
{
    /// <summary>
    /// Busca por trecho do nome do projeto ou do cliente, sem diferenciar maiúsculas.
    /// Projeto e Cliente são agregados distintos, por isso o cliente entra como subconsulta.
    /// </summary>
    public static IQueryable<Projeto> ComTermo(this IQueryable<Projeto> query, string? termo, IQueryable<Cliente> clientes)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return query;
        }

#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var termoMinusculo = termo.Trim().ToLower();
        return query.Where(p => p.Nome.ToLower().Contains(termoMinusculo)
            || clientes.Any(c => c.Id == p.ClienteId && c.Nome.ToLower().Contains(termoMinusculo)));
#pragma warning restore CA1304, CA1311, CA1862
    }
}
