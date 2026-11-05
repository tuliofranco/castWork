using Castwork.Domain.Entities.Pecas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations;

internal sealed class ObservacaoPecaConfiguration : IEntityTypeConfiguration<ObservacaoPeca>
{
    public void Configure(EntityTypeBuilder<ObservacaoPeca> builder)
    {
        builder.ToTable("peca_observacoes");
        builder.HasKey(o => o.Id);
        builder.IgnorarEventos();

        builder.Property(o => o.Texto).HasMaxLength(ObservacaoPeca.TextoMaxLength).IsRequired();
        builder.Property(o => o.StatusDaPeca).IsRequired();
        builder.Property(o => o.CriadaEm).IsRequired();

        builder.HasIndex("PecaId", nameof(ObservacaoPeca.CriadaEm));
    }
}
