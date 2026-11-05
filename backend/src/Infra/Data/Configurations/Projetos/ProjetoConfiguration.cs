using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class ProjetoConfiguration : IEntityTypeConfiguration<Projeto>
{
    public void Configure(EntityTypeBuilder<Projeto> builder)
    {
        builder.ToTable("projetos");
        builder.HasKey(p => p.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(p => p.Nome).HasMaxLength(Projeto.NomeMaxLength).IsRequired();
        builder.Property(p => p.Descricao).HasMaxLength(Projeto.DescricaoMaxLength);

        // Cliente com projetos não é excluído, é desativado.
        builder.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Versoes).WithOne().HasForeignKey("ProjetoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Versoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Nome);
    }
}
