using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Atelie;

internal sealed class ConfiguracaoAtelieConfiguration : IEntityTypeConfiguration<ConfiguracaoAtelie>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoAtelie> builder)
    {
        builder.ToTable("configuracao_atelie");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(c => c.Nome).HasMaxLength(ConfiguracaoAtelie.NomeMaxLength).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(ConfiguracaoAtelie.EmailMaxLength);
        builder.Property(c => c.Telefone).HasMaxLength(ConfiguracaoAtelie.TelefoneMaxLength);
        builder.Property(c => c.LogoArquivo).HasMaxLength(ConfiguracaoAtelie.LogoMaxLength);
        builder.Property(c => c.ValorHoraPadrao).IsRequired();
        builder.ComplexProperty(c => c.Endereco, e => e.ConfigurarEndereco());

        // A única linha nasce com a migration: o ateliê só preenche os dados depois.
        builder.HasData(new
        {
            Id = ConfiguracaoAtelie.IdUnico,
            Nome = "Meu ateliê",
            ValorHoraPadrao = Dinheiro.Zero,
            CriadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
