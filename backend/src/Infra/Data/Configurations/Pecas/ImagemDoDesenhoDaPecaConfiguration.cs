using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Pecas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Pecas;

internal sealed class ImagemDoDesenhoDaPecaConfiguration : IEntityTypeConfiguration<ImagemDoDesenhoDaPeca>
{
    public void Configure(EntityTypeBuilder<ImagemDoDesenhoDaPeca> builder)
    {
        builder.ToTable("peca_desenho_imagens");
        builder.HasKey(i => i.Id);
        builder.IgnorarEventos();

        builder.Property<long>("DesenhoId");
        builder.Property(i => i.Vista).HasMaxLength(VistaDaImagem.MaxLength).IsRequired();
        builder.Property(i => i.Tipo).HasMaxLength(TipoDaImagemGuardada.MaxLength).IsRequired();
        builder.Property(i => i.Conteudo).IsRequired();

        builder.HasIndex("DesenhoId", nameof(ImagemDoDesenhoDaPeca.Vista), nameof(ImagemDoDesenhoDaPeca.Tipo)).IsUnique();
    }
}
