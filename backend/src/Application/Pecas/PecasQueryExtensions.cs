using Castwork.Domain.Entities.Pecas;

namespace Castwork.Application.Pecas;

/// <summary>
/// Filtros reutilizáveis sobre peças. Cada um é no-op quando o parâmetro vem vazio,
/// então podem ser encadeados direto com os campos opcionais da query.
/// </summary>
public static class PecasQueryExtensions
{
    /// <summary>
    /// Busca por título ou nome do cliente, sem diferenciar maiúsculas (vira <c>lower(...) LIKE</c> no PostgreSQL).
    /// </summary>
    public static IQueryable<Peca> ComTermo(this IQueryable<Peca> query, string? termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return query;
        }

#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var termoMinusculo = termo.Trim().ToLower();
        return query.Where(p => p.Titulo.ToLower().Contains(termoMinusculo)
            || (p.Cliente != null && p.Cliente.Nome.ToLower().Contains(termoMinusculo)));
#pragma warning restore CA1304, CA1311, CA1862
    }

    public static IQueryable<Peca> ComStatus(this IQueryable<Peca> query, StatusPeca? status)
        => status is null ? query : query.Where(p => p.Status == status);

    public static IQueryable<Peca> DoCliente(this IQueryable<Peca> query, long? clienteId)
        => clienteId is null ? query : query.Where(p => p.ClienteId == clienteId);
}
