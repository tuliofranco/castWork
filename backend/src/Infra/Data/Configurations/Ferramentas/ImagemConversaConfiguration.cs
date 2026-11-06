using Castwork.Domain.Entities.Ferramentas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ferramentas;

internal sealed class ImagemConversaConfiguration : IEntityTypeConfiguration<ImagemConversa>
{
    public void Configure(EntityTypeBuilder<ImagemConversa> builder)
    {
        builder.ToTable("ferramenta_imagens");
        builder.HasKey(i => i.Id);
        builder.IgnorarEventos();

        builder.HasOne<ConversaFerramenta>().WithMany().HasForeignKey(i => i.ConversaId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(i => i.Chave).IsRequired();
        builder.Property(i => i.Tipo).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Conteudo).IsRequired();
        builder.Property(i => i.CriadaEm).IsRequired();

        builder.HasIndex(i => i.Chave).IsUnique();
        builder.HasIndex(i => i.ConversaId);
    }
}
