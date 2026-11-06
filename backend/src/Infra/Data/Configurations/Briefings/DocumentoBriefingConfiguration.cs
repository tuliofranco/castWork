using Castwork.Domain.Entities.Ferramentas.Briefings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Briefings;

internal sealed class DocumentoBriefingConfiguration : IEntityTypeConfiguration<DocumentoBriefing>
{
    public void Configure(EntityTypeBuilder<DocumentoBriefing> builder)
    {
        builder.ToTable("briefing_documentos");
        builder.HasKey(d => d.Id);
        builder.IgnorarEventos();

        builder.HasOne<Briefing>().WithMany().HasForeignKey(d => d.BriefingId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(d => d.NomeArquivo).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Conteudo).IsRequired();
        builder.Property(d => d.GeradoEm).IsRequired();

        builder.HasIndex(d => new { d.BriefingId, d.GeradoEm });
    }
}
