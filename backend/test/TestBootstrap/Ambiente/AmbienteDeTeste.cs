using Castwork.Infra.Data;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Respawn;
using Respawn.Graph;

using Testcontainers.PostgreSql;

namespace Castwork.TestBootstrap.Ambiente;

/// <summary>
/// Ambiente compartilhado pelos testes de integração: um PostgreSQL em container (porta aleatória,
/// removido no fim), a API completa via <see cref="CastworkWebApplicationFactory"/> e o Respawn para
/// zerar as tabelas entre testes sem recriar o banco.
/// </summary>
public static class AmbienteDeTeste
{
    private const string ImagemPostgres = "postgres:17-alpine";

    private static PostgreSqlContainer? _container;
    private static CastworkWebApplicationFactory? _factory;
    private static Respawner? _respawner;

    public static RelogioDeTeste Relogio { get; } = new();

    public static SessaoDeTeste Sessao { get; } = new();

    private static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Ambiente de teste não iniciado.");

    private static IServiceScopeFactory ScopeFactory => _factory?.Services.GetRequiredService<IServiceScopeFactory>()
        ?? throw new InvalidOperationException("Ambiente de teste não iniciado.");

    public static async Task IniciarAsync()
    {
        _container = new PostgreSqlBuilder(ImagemPostgres).Build();
        await _container.StartAsync();

        _factory = new CastworkWebApplicationFactory(ConnectionString, Relogio, Sessao);

        // Acessar Services executa o Program.cs inteiro, inclusive as migrations.
        _ = _factory.Services;

        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();
        _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],

            // regras: catálogo sincronizado na inicialização; configuracao_atelie: linha única recriada no reset.
            TablesToIgnore = [new Table("__EFMigrationsHistory"), new Table("regras")],
        });
    }

    public static async Task EncerrarAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Banco vazio, relógio no início e usuário padrão. Chamado antes de cada teste.
    /// </summary>
    public static async Task ResetarAsync()
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();
        await _respawner!.ResetAsync(conexao);
        await RecriarConfiguracaoAtelieAsync(conexao);

        Relogio.Reiniciar();
        Sessao.Reiniciar();
    }

    /// <summary>
    /// Cliente HTTP para a API em memória (passa pelo pipeline ASP.NET inteiro, inclusive o JwtBearer).
    /// </summary>
    public static HttpClient CriarClienteHttp()
        => _factory?.CreateClient() ?? throw new InvalidOperationException("Ambiente de teste não iniciado.");

    /// <summary>
    /// Outra instância da API sobre o mesmo banco, com configuração própria (ex.: rate limit baixo).
    /// Quem chama descarta a factory.
    /// </summary>
    public static CastworkWebApplicationFactory CriarFactory(IReadOnlyDictionary<string, string> configuracao)
        => new(ConnectionString, Relogio, Sessao, configuracao);

    public static T Obter<T>()
        where T : notnull
        => _factory!.Services.GetRequiredService<T>();

    /// <summary>
    /// Envia pelo MediatR real, num escopo novo (como uma requisição HTTP).
    /// </summary>
    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = ScopeFactory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>
    /// Variante para commands sem retorno.
    /// </summary>
    public static async Task SendAsync(IRequest request)
    {
        using var scope = ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public static async Task<T> ExecutarNoEscopoAsync<T>(Func<IServiceProvider, Task<T>> acao)
    {
        using var scope = ScopeFactory.CreateScope();
        return await acao(scope.ServiceProvider);
    }

    /// <summary>
    /// Acesso direto ao DbContext, para o Assert ler o que foi gravado (ou o Arrange montar um cenário raro).
    /// </summary>
    public static Task<T> ExecutarNoBancoAsync<T>(Func<ApplicationDbContext, Task<T>> acao)
        => ExecutarNoEscopoAsync(sp => acao(sp.GetRequiredService<ApplicationDbContext>()));

    public static Task ExecutarNoBancoAsync(Func<ApplicationDbContext, Task> acao)
        => ExecutarNoBancoAsync(async context =>
        {
            await acao(context);
            return true;
        });

    private static async Task RecriarConfiguracaoAtelieAsync(NpgsqlConnection conexao)
    {
        await using var comando = new NpgsqlCommand(
            "INSERT INTO configuracao_atelie (id, nome, valor_hora_padrao, criado_em) VALUES (1, 'Meu ateliê', 0, '2026-01-01T00:00:00Z')",
            conexao);
        await comando.ExecuteNonQueryAsync();
    }
}
