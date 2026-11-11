using Castwork.Domain.Entities.Pecas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Pecas;

internal sealed class DesenhoDaPecaConfiguration : IEntityTypeConfiguration<DesenhoDaPeca>
{
    public void Configure(EntityTypeBuilder<DesenhoDaPeca> builder)
    {
        builder.ToTable("peca_desenhos");
        builder.HasKey(d => d.Id);
        builder.IgnorarEventos();

        builder.Property<long>("PecaId");
        builder.Property(d => d.Ferramenta).HasMaxLength(DesenhoDaPeca.FerramentaMaxLength).IsRequired();
        builder.Property(d => d.Conteudo).IsRequired();
        builder.Property(d => d.CopiadoEm).IsRequired();

        // Um desenho por ferramenta em cada peça.
        builder.HasIndex("PecaId", nameof(DesenhoDaPeca.Ferramenta)).IsUnique();

        builder.HasMany(d => d.Imagens).WithOne().HasForeignKey("DesenhoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(d => d.Imagens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
