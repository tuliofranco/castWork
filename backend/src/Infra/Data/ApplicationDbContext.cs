using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;
using Castwork.Infra.Data.Converters;

using Microsoft.EntityFrameworkCore;

namespace Castwork.Infra.Data;

/// <summary>
/// Schema nasce do domínio + <c>Configurations/</c>. Nunca editar migration gerada à mão:
/// <c>migrations remove</c> → corrigir a configuration → <c>migrations add</c>.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Peca> Pecas => Set<Peca>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Perfil> Perfis => Set<Perfil>();

    public DbSet<Regra> Regras => Set<Regra>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ConfiguracaoAtelie> ConfiguracaoAtelie => Set<ConfiguracaoAtelie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Convenções globais: todo value object e enum de domínio tem um único formato no banco,
    /// definido aqui uma vez em vez de repetido em cada configuration.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Dinheiro>().HaveConversion<DinheiroConverter>().HavePrecision(18, 2);
        configurationBuilder.Properties<Peso>().HaveConversion<PesoConverter>().HavePrecision(10, 3);
        configurationBuilder.Properties<TeorMetal>().HaveConversion<TeorMetalConverter>();
        configurationBuilder.Properties<Documento>().HaveConversion<DocumentoConverter>().HaveMaxLength(14);
        configurationBuilder.Properties<StatusPeca>().HaveConversion<StatusPecaConverter>().HaveMaxLength(1).AreFixedLength();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
