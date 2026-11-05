using Castwork.Domain.Entities.Pecas;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations;

internal sealed class EtapaPecaConfiguration : IEntityTypeConfiguration<EtapaPeca>
{
    public void Configure(EntityTypeBuilder<EtapaPeca> builder)
    {
        builder.ToTable("peca_etapas");
        builder.HasKey(e => e.Id);
        builder.IgnorarEventos();
        builder.Ignore(e => e.EmAndamento);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.IniciadaEm).IsRequired();

        builder.HasIndex("PecaId", nameof(EtapaPeca.IniciadaEm));
    }
}
