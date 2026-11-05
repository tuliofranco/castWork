using System.Linq.Expressions;
using System.Reflection;

namespace Castwork.Application.Common.Pagination;

/// <summary>
/// Ordenação pelo nome de uma propriedade do DTO, montada como expression tree para virar ORDER BY no SQL.
/// </summary>
public static class OrdenacaoDinamica
{
    public static PropertyInfo? EncontrarPropriedade<T>(string nome)
        => typeof(T).GetProperty(nome, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    public static bool EhCampoValido<T>(string? nome)
        => string.IsNullOrWhiteSpace(nome) || EncontrarPropriedade<T>(nome) is not null;

    internal static IOrderedQueryable<T> Ordenar<T>(IQueryable<T> query, string nome, bool decrescente)
    {
        var propriedade = EncontrarPropriedade<T>(nome)
            ?? throw new ArgumentException($"'{nome}' não é um campo de ordenação de {typeof(T).Name}.", nameof(nome));

        var parametro = Expression.Parameter(typeof(T), "x");
        var chave = Expression.Lambda(Expression.Property(parametro, propriedade), parametro);
        var metodo = decrescente ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

        var chamada = Expression.Call(
            typeof(Queryable),
            metodo,
            [typeof(T), propriedade.PropertyType],
            query.Expression,
            Expression.Quote(chave));

        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(chamada);
    }
}
