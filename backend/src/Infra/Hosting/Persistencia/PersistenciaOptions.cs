using System.ComponentModel.DataAnnotations;

namespace Castwork.Infra.Hosting.Persistencia;

/// <summary>
/// Duas credenciais de banco (menor privilégio): <see cref="Castwork"/> só lê e escreve dados (API);
/// <see cref="CastworkMigrations"/> tem DDL e é usada apenas para aplicar migrations.
/// Valores vêm de User Secrets / variáveis de ambiente, nunca do appsettings versionado.
/// </summary>
public sealed class PersistenciaOptions
{
    public const string Secao = "ConnectionStrings";

    [Required(ErrorMessage = "ConnectionStrings:Castwork não configurada (use dotnet user-secrets ou a variável ConnectionStrings__Castwork).")]
    public string Castwork { get; init; } = string.Empty;

    /// <summary>
    /// Opcional. Se ausente, as migrations não são aplicadas na inicialização (cenário de produção com efbundle).
    /// </summary>
    public string? CastworkMigrations { get; init; }
}
