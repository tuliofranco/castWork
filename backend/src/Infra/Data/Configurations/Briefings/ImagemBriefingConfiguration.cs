using Castwork.Domain.Entities.Briefings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Briefings;

internal sealed class ImagemBriefingConfiguration : IEntityTypeConfiguration<ImagemBriefing>
{
    public void Configure(EntityTypeBuilder<ImagemBriefing> builder)
    {
        builder.ToTable("briefing_imagens");
        builder.HasKey(i => i.Id);
        builder.IgnorarEventos();

        builder.HasOne<Briefing>().WithMany().HasForeignKey(i => i.BriefingId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(i => i.Chave).IsRequired();
        builder.Property(i => i.Tipo).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Conteudo).IsRequired();
        builder.Property(i => i.EnviadaEm).IsRequired();

        builder.HasIndex(i => i.Chave).IsUnique();
        builder.HasIndex(i => i.BriefingId);
    }
}
