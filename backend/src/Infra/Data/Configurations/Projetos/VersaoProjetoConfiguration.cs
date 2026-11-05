using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class VersaoProjetoConfiguration : IEntityTypeConfiguration<VersaoProjeto>
{
    public void Configure(EntityTypeBuilder<VersaoProjeto> builder)
    {
        builder.ToTable("projeto_versoes");
        builder.HasKey(v => v.Id);
        builder.IgnorarEventos();

        builder.Property<long>("ProjetoId");
        builder.Property(v => v.Nome).HasMaxLength(VersaoProjeto.NomeMaxLength).IsRequired();
        builder.Property(v => v.Observacoes).HasMaxLength(VersaoProjeto.ObservacoesMaxLength);
        builder.Property(v => v.Status).IsRequired();

        builder.HasMany(v => v.Itens).WithOne().HasForeignKey("VersaoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Garante no banco a numeração sem repetição dentro do projeto.
        builder.HasIndex("ProjetoId", nameof(VersaoProjeto.Numero)).IsUnique();
    }
}
