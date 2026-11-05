using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Acesso;
using Castwork.Infra.Data;
using Castwork.Infra.Data.Seed;
using Castwork.Infra.Hosting.Persistencia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Castwork.Infra.Hosting.Inicializacao;

/// <summary>
/// Preparação do banco e dos dados mínimos na subida da API.
/// </summary>
public static partial class InicializacaoExtensions
{
    /// <summary>
    /// Tudo que precisa acontecer antes de a API aceitar requisições, na ordem:
    /// migrations → catálogo de regras → primeiro administrador.
    /// </summary>
    public static async Task InicializarCastworkAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await services.AplicarMigrationsAsync(cancellationToken);

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(InicializacaoExtensions));

        var resultado = await SincronizadorDeRegras.SincronizarAsync(context, cancellationToken);
        LogRegras(logger, resultado.Inseridas, resultado.Atualizadas, resultado.Removidas);

        await CriarAdminInicialAsync(scope.ServiceProvider, context, logger, cancellationToken);
    }

    private static async Task CriarAdminInicialAsync(
        IServiceProvider services,
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await context.Usuarios.AnyAsync(cancellationToken))
        {
            return;
        }

        var admin = services.GetRequiredService<IConfiguration>().GetSection(AdminInicialOptions.Secao).Get<AdminInicialOptions>();
        if (admin is null || !admin.Configurado)
        {
            LogSemAdmin(logger);
            return;
        }

        var hash = services.GetRequiredService<ISenhaHasher>().Hash(admin.Senha!);
        context.Usuarios.Add(Usuario.Criar(admin.Nome ?? "Administrador", admin.Email!, hash, perfilId: null, isAdmin: true));
        await context.SaveChangesAsync(cancellationToken);
        LogAdminCriado(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Regras sincronizadas: {Inseridas} inseridas, {Atualizadas} atualizadas, {Removidas} removidas.")]
    private static partial void LogRegras(ILogger logger, int inseridas, int atualizadas, int removidas);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Nenhum usuário cadastrado e Acesso:AdminInicial não configurado: ninguém consegue entrar.")]
    private static partial void LogSemAdmin(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Administrador inicial criado. Troque a senha no primeiro acesso e remova Acesso:AdminInicial da configuração.")]
    private static partial void LogAdminCriado(ILogger logger);
}
