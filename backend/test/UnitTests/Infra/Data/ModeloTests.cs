using Castwork.Infra.Data;
using Castwork.Infra.Hosting.Persistencia;

using Microsoft.EntityFrameworkCore;

namespace Castwork.UnitTests.Infra.Data;

/// <summary>
/// Garante que toda mudança no domínio/configurations veio acompanhada de migration.
/// Não precisa de banco: compara o modelo atual com o snapshot do projeto Migrations.
/// </summary>
public class ModeloTests
{
    [Test]
    public void DeveModeloEstarSincronizadoComAsMigrations()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        PersistenciaExtensions.Configurar(options, "Host=localhost;Database=castwork_modelo");
        using var context = new ApplicationDbContext(options.Options);

        var pendente = context.Database.HasPendingModelChanges();

        pendente.Should().BeFalse("o modelo mudou sem migration: rode 'dotnet ef migrations add <Nome>'");
    }

    [Test]
    public void DeveUsarSnakeCaseEmTabelasEColunas()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        PersistenciaExtensions.Configurar(options, "Host=localhost;Database=castwork_modelo");
        using var context = new ApplicationDbContext(options.Options);

        var nomes = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties().Select(p => p.GetColumnName()).Append(e.GetTableName()!))
            .ToList();

        nomes.Should().AllSatisfy(n => n.Should().MatchRegex("^[a-z][a-z0-9_]*$"));
    }
}
