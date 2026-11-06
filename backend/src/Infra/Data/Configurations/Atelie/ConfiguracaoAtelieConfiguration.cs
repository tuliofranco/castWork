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
        builder.Property(c => c.Assinatura).HasMaxLength(ConfiguracaoAtelie.AssinaturaMaxLength);
        builder.Property(c => c.CorMarca).HasMaxLength(7).IsFixedLength().IsRequired().HasDefaultValue(ConfiguracaoAtelie.CorPadrao);
        builder.Property(c => c.Instagram).HasMaxLength(ConfiguracaoAtelie.InstagramMaxLength);
        builder.Property(c => c.Site).HasMaxLength(ConfiguracaoAtelie.SiteMaxLength);
        builder.Property(c => c.AvisoBriefing).HasMaxLength(ConfiguracaoAtelie.AvisoMaxLength);
        builder.Property(c => c.Logo);
        builder.Property(c => c.LogoTipo).HasMaxLength(20);
        builder.Property(c => c.ValorHoraPadrao).IsRequired();
        builder.ComplexProperty(c => c.Endereco, e => e.ConfigurarEndereco());

        // A única linha nasce com a migration: o ateliê só preenche os dados depois.
        builder.HasData(new
        {
            Id = ConfiguracaoAtelie.IdUnico,
            Nome = "Meu ateliê",
            CorMarca = ConfiguracaoAtelie.CorPadrao,
            AvisoBriefing = ConfiguracaoAtelie.AvisoPadrao,
            ValorHoraPadrao = Dinheiro.Zero,
            CriadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
