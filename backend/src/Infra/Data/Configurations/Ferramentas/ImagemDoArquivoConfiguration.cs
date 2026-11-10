using Castwork.Domain.Entities.Assistente;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ferramentas;

internal sealed class ImagemDoArquivoConfiguration : IEntityTypeConfiguration<ImagemDoArquivo>
{
    public void Configure(EntityTypeBuilder<ImagemDoArquivo> builder)
    {
        builder.ToTable("ferramenta_arquivo_imagens");
        builder.HasKey(i => i.Id);
        builder.IgnorarEventos();

        builder.HasOne<ArquivoFerramenta>().WithMany().HasForeignKey(i => i.ArquivoId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(i => i.Vista).HasMaxLength(VistaDaImagem.MaxLength).IsRequired();
        builder.Property(i => i.Tipo).HasMaxLength(TipoDaImagemGuardada.MaxLength).IsRequired();
        builder.Property(i => i.Conteudo).IsRequired();
        builder.Property(i => i.CriadaEm).IsRequired();

        // Uma imagem por vista e tipo em cada arquivo (o 2D tem o SVG e o PNG).
        builder.HasIndex(i => new { i.ArquivoId, i.Vista, i.Tipo }).IsUnique();
    }
}
