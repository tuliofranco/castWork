using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Pecas;

internal sealed class PecaConfiguration : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> builder)
    {
        builder.ToTable("pecas");
        builder.HasKey(p => p.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(p => p.Nome).HasMaxLength(Peca.NomeMaxLength).IsRequired();
        builder.Property(p => p.Tipo).IsRequired();
        builder.Property(p => p.Material).HasMaxLength(Peca.MaterialMaxLength);
        builder.Property(p => p.Teor);

        // A liga escolhida: o nome e o teor dela ficam copiados em material e teor. Liga em uso não é excluída.
        builder.HasOne<Liga>().WithMany().HasForeignKey(p => p.LigaId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.Peso);
        builder.Property(p => p.Descricao).HasMaxLength(Peca.DescricaoMaxLength);
        builder.Property(p => p.Ativo).IsRequired();

        // O desenho 2D e o modelo 3D copiados ao publicar: carregados só quando pedidos (o conteúdo é grande).
        builder.HasMany(p => p.Desenhos).WithOne().HasForeignKey("PecaId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Desenhos).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.Tipo);
    }
}
