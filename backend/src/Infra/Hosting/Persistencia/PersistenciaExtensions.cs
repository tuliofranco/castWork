using Castwork.Application.Abstractions;
using Castwork.Infra.Data;
using Castwork.Infra.Data.Interceptors;
using Castwork.Infra.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.Hosting.Persistencia;

public static class PersistenciaExtensions
{
    private static readonly string MigrationsAssembly = typeof(Migrations.AssemblyReference).Assembly.GetName().Name!;

    public static IServiceCollection AddPersistencia(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PersistenciaOptions>()
            .Bind(configuration.GetSection(PersistenciaOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<AuditoriaInterceptor>();
        services.AddScoped<ImagensDoArquivoInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IOptions<PersistenciaOptions>>().Value.Castwork;
            Configurar(options, connectionString).AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>(), sp.GetRequiredService<ImagensDoArquivoInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes com a credencial de DDL. Roda antes de a API aceitar requisições.
    /// Sem <c>ConnectionStrings:CastworkMigrations</c>, não faz nada: o deploy aplica via <c>efbundle</c>.
    /// </summary>
    public static async Task AplicarMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        var options = services.GetRequiredService<IOptions<PersistenciaOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PersistenciaExtensions));

        if (string.IsNullOrWhiteSpace(options.CastworkMigrations))
        {
            logger.LogInformation("Migrations automáticas desligadas (sem ConnectionStrings:CastworkMigrations).");
            return;
        }

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        Configurar(builder, options.CastworkMigrations);

        await using var context = new ApplicationDbContext(builder.Options);
        var pendentes = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendentes.Count == 0)
        {
            logger.LogInformation("Banco atualizado; nenhuma migration pendente.");
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Aplicando {Quantidade} migration(s): {Migrations}", pendentes.Count, pendentes);
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Configuração do provider compartilhada pela API, pelas migrations e pelos testes de modelo.
    /// </summary>
    public static DbContextOptionsBuilder Configurar(DbContextOptionsBuilder options, string connectionString)
        => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(MigrationsAssembly);

                // Repete em falhas transitórias (queda de conexão, failover). Transações explícitas precisarão
                // de CreateExecutionStrategy(); o SaveChanges único por command não precisa.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5);
            })

            // Tabelas e colunas em snake_case (pecas, cliente_id): o padrão do PostgreSQL, que dispensa aspas no SQL manual.
            .UseSnakeCaseNamingConvention();
}
