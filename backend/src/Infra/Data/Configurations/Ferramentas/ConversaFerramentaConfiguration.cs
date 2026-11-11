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

        // Uma conversa por ferramenta em cada versão, em cada peça do projeto (ou no projeto, sem as duas); some junto com
        // a versão, a peça ou o projeto.
        builder.HasOne<Projeto>().WithMany().HasForeignKey(c => c.ProjetoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VersaoProjeto>().WithMany().HasForeignKey(c => c.VersaoId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PecaDoProjeto>().WithMany().HasForeignKey(c => c.PecaDoProjetoId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

        // O arquivo final da peça; apagar o arquivo (só arquivado, e o final não arquiva) deixa a peça sem final.
        builder.HasOne<ArquivoFerramenta>().WithMany().HasForeignKey(c => c.ArquivoFinalId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.Ferramenta).HasMaxLength(ConversaFerramenta.FerramentaMaxLength).IsRequired();
        builder.Property(c => c.CriadaEm).IsRequired();
        builder.Property(c => c.ChatAtual).IsRequired();
        builder.Ignore(c => c.MensagensDoChat);
        builder.HasIndex(c => new { c.ProjetoId, c.VersaoId, c.PecaDoProjetoId, c.Ferramenta }).IsUnique().AreNullsDistinct(false);

        builder.HasMany(c => c.Mensagens).WithOne().HasForeignKey("ConversaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Mensagens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
