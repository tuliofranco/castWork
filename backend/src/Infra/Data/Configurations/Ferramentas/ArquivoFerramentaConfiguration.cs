using Castwork.Domain.Entities.Ferramentas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ferramentas;

internal sealed class ArquivoFerramentaConfiguration : IEntityTypeConfiguration<ArquivoFerramenta>
{
    public void Configure(EntityTypeBuilder<ArquivoFerramenta> builder)
    {
        builder.ToTable("ferramenta_arquivos");
        builder.HasKey(a => a.Id);
        builder.IgnorarEventos();

        builder.HasOne<ConversaFerramenta>().WithMany().HasForeignKey(a => a.ConversaId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(a => a.Conteudo).IsRequired();
        builder.Property(a => a.Origem).IsRequired();
        builder.Property(a => a.CriadoEm).IsRequired();

        builder.HasIndex(a => new { a.ConversaId, a.Id });
    }
}
