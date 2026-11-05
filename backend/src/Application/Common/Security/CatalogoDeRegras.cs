using System.Reflection;

namespace Castwork.Application.Common.Security;

public sealed record RegraDoCatalogo(Guid Id, string Codigo, string Grupo, string Descricao);

/// <summary>
/// Leitura do catálogo <see cref="Regras"/> como dados: sincroniza a tabela de regras, valida as regras de um perfil
/// e traduz GUIDs em códigos para o JWT.
/// </summary>
public static class CatalogoDeRegras
{
    public static IReadOnlyList<RegraDoCatalogo> Todas { get; } =
    [
        .. typeof(Regras).GetNestedTypes(BindingFlags.Public)
            .Where(grupo => grupo.IsAbstract && grupo.IsSealed)
            .SelectMany(grupo => grupo.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(campo => campo.IsLiteral)
                .Select(campo =>
                {
                    var regra = campo.GetCustomAttribute<Regras.RegraAttribute>()
                        ?? throw new InvalidOperationException($"Regras.{grupo.Name}.{campo.Name} sem [Regra].");
                    return new RegraDoCatalogo(Guid.Parse((string)campo.GetRawConstantValue()!), regra.Codigo, grupo.Name, regra.Descricao);
                })),
    ];

    public static bool Existe(Guid regra) => Todas.Any(r => r.Id == regra);

    /// <summary>
    /// Códigos das regras informadas, ignorando as que não estão mais no catálogo.
    /// </summary>
    public static IReadOnlyList<string> Codigos(IEnumerable<Guid> regras)
    {
        var ids = regras.ToHashSet();
        return [.. Todas.Where(r => ids.Contains(r.Id)).Select(r => r.Codigo)];
    }

    public static IReadOnlyList<string> TodosOsCodigos() => [.. Todas.Select(r => r.Codigo)];
}
