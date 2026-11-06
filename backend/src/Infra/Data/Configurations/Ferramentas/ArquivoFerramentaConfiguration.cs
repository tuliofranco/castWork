using Castwork.Domain.Entities.Assistente;

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
        builder.Property(a => a.Nome).HasMaxLength(ArquivoFerramenta.NomeMaxLength);
        builder.Property(a => a.ArquivadoEm);

        builder.HasIndex(a => new { a.ConversaId, a.Id });
    }
}
