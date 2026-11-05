using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Acesso;

internal sealed class RegraConfiguration : IEntityTypeConfiguration<Regra>
{
    public void Configure(EntityTypeBuilder<Regra> builder)
    {
        builder.ToTable("regras");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Codigo).HasMaxLength(Regra.CodigoMaxLength).IsRequired();
        builder.HasIndex(r => r.Codigo).IsUnique();
        builder.Property(r => r.Grupo).HasMaxLength(Regra.GrupoMaxLength).IsRequired();
        builder.Property(r => r.Descricao).HasMaxLength(Regra.DescricaoMaxLength).IsRequired();
    }
}
