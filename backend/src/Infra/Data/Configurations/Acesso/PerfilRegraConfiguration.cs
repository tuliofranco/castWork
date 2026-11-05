using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Acesso;

internal sealed class PerfilRegraConfiguration : IEntityTypeConfiguration<PerfilRegra>
{
    public void Configure(EntityTypeBuilder<PerfilRegra> builder)
    {
        builder.ToTable("perfil_regras");
        builder.HasKey(r => r.Id);
        builder.IgnorarEventos();

        // Restrict: uma regra removida do catálogo não some em silêncio dos perfis; a sincronização trata.
        builder.HasOne<Regra>().WithMany().HasForeignKey(r => r.RegraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex("PerfilId", nameof(PerfilRegra.RegraId)).IsUnique();
    }
}
