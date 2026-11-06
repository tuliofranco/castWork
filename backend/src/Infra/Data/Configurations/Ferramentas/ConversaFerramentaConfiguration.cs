using Castwork.Domain.Entities.Assistente;
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

        // Uma conversa por ferramenta em cada versão (ou no projeto, sem versão); some junto com a versão ou o projeto.
        builder.HasOne<Projeto>().WithMany().HasForeignKey(c => c.ProjetoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VersaoProjeto>().WithMany().HasForeignKey(c => c.VersaoId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        builder.Property(c => c.Ferramenta).HasMaxLength(ConversaFerramenta.FerramentaMaxLength).IsRequired();
        builder.Property(c => c.CriadaEm).IsRequired();
        builder.Property(c => c.ChatAtual).IsRequired();
        builder.Ignore(c => c.MensagensDoChat);
        builder.HasIndex(c => new { c.ProjetoId, c.VersaoId, c.Ferramenta }).IsUnique().AreNullsDistinct(false);

        builder.HasMany(c => c.Mensagens).WithOne().HasForeignKey("ConversaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Mensagens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
