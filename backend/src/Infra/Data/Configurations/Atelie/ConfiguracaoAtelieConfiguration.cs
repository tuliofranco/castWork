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
        builder.ComplexProperty(c => c.Endereco, e => e.ConfigurarEndereco());

        // Os padrões de preço entram como default das colunas: valem para a linha que já existe e para a recriada.
        // valor_hora_padrao é a coluna de antes (o valor da hora já era do ateliê) e começa em zero, "a definir".
        builder.ComplexProperty(c => c.ParametrosDePreco, p =>
        {
            p.Property(x => x.PerdaPercentual).HasColumnName("perda_percentual").HasPrecision(5, 2).HasDefaultValue(ParametrosDePreco.PerdaPadrao);
            p.Property(x => x.ValorHora).HasColumnName("valor_hora_padrao").HasDefaultValue(Dinheiro.Zero);
            p.Property(x => x.Markup).HasColumnName("markup").HasPrecision(5, 2).HasDefaultValue(ParametrosDePreco.MarkupPadrao);
            p.Property(x => x.TaxasPercentual).HasColumnName("taxas_percentual").HasPrecision(5, 2).HasDefaultValue(ParametrosDePreco.TaxasPadrao);
            p.Property(x => x.ArredondarPara).HasColumnName("arredondar_para")
                .HasDefaultValue(Dinheiro.Criar(ParametrosDePreco.ArredondamentoPadrao));
        });

        // A única linha nasce com a migration: o ateliê só preenche os dados depois.
        builder.HasData(new
        {
            Id = ConfiguracaoAtelie.IdUnico,
            Nome = "Meu ateliê",
            CorMarca = ConfiguracaoAtelie.CorPadrao,
            AvisoBriefing = ConfiguracaoAtelie.AvisoPadrao,
            CriadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
