using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Acesso;

internal sealed class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("perfis");
        builder.HasKey(p => p.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(p => p.Nome).HasMaxLength(Perfil.NomeMaxLength).IsRequired();
        builder.HasIndex(p => p.Nome).IsUnique();

        builder.HasMany(p => p.Regras).WithOne().HasForeignKey("PerfilId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Regras).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
