using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ferramentas;

internal sealed class ConversaFerramentaConfiguration : IEntityTypeConfiguration<ConversaFerramenta>
{
    public void Configure(EntityTypeBuilder<ConversaFerramenta> builder)
    {
        builder.ToTable("ferramenta_conversas");
        builder.HasKey(c => c.Id);
        builder.IgnorarEventos().ConfigurarConcorrencia();

        // Uma conversa por ferramenta em cada versão; some junto com a versão.
        builder.HasOne<VersaoProjeto>().WithMany().HasForeignKey(c => c.VersaoId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(c => c.Ferramenta).HasMaxLength(ConversaFerramenta.FerramentaMaxLength).IsRequired();
        builder.Property(c => c.CriadaEm).IsRequired();
        builder.HasIndex(c => new { c.VersaoId, c.Ferramenta }).IsUnique();

        builder.HasMany(c => c.Mensagens).WithOne().HasForeignKey("ConversaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Mensagens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
