using Castwork.Domain.Entities.Pecas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations;

internal sealed class PecaConfiguration : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> builder)
    {
        builder.ToTable("pecas");
        builder.HasKey(p => p.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();
        builder.Ignore(p => p.EtapaAtual);
        builder.Ignore(p => p.EstaEncerrada);
        builder.Ignore(p => p.ProximosStatus);

        builder.Property(p => p.Titulo).HasMaxLength(Peca.TituloMaxLength).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(Peca.DescricaoMaxLength);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.MotivoCancelamento).HasMaxLength(Peca.MotivoMaxLength);

        // Restrict: cliente com histórico de peças não é apagado, é desativado.
        builder.HasOne(p => p.Cliente).WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // Etapas e observações pertencem ao agregado: carregadas e removidas junto com a peça.
        builder.HasMany(p => p.Etapas).WithOne().HasForeignKey("PecaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Observacoes).WithOne().HasForeignKey("PecaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Observacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PrazoEntrega);
    }
}
