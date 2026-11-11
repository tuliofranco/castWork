using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class PecaDoProjetoConfiguration : IEntityTypeConfiguration<PecaDoProjeto>
{
    /// <summary>
    /// Navegação privada da <see cref="PecaDoProjeto"/> para a peça criada ao publicar.
    /// </summary>
    private const string PecaPublicada = "PecaPublicada";

    public void Configure(EntityTypeBuilder<PecaDoProjeto> builder)
    {
        builder.ToTable("projeto_pecas");
        builder.HasKey(p => p.Id);
        builder.IgnorarEventos();

        builder.Property<long>("ProjetoId");
        builder.Property(p => p.Nome).HasMaxLength(Peca.NomeMaxLength).IsRequired();
        builder.Property(p => p.Tipo).IsRequired();
        builder.Property(p => p.Material).HasMaxLength(Peca.MaterialMaxLength);
        builder.Property(p => p.Teor);
        builder.Property(p => p.Peso);
        builder.Property(p => p.Descricao).HasMaxLength(Peca.DescricaoMaxLength);
        builder.Property(p => p.Ativo).IsRequired();
        builder.Property(p => p.Valor);
        builder.Property(p => p.Status).IsRequired();

        // A liga escolhida: o nome e o teor dela ficam copiados em material e teor. Liga em uso não é excluída.
        builder.HasOne<Liga>().WithMany().HasForeignKey(p => p.LigaId).OnDelete(DeleteBehavior.Restrict);

        // Vínculo com a biblioteca. A navegação privada deixa a peça nova (ao publicar) e o vínculo saírem no mesmo
        // SaveChanges; peça da biblioteca usada por algum projeto não é excluída (é desativada).
        builder.HasOne<Peca>(PecaPublicada).WithMany().HasForeignKey(p => p.PecaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(PecaPublicada).UsePropertyAccessMode(PropertyAccessMode.Property);

        // Histórico de uso da peça da biblioteca: as cópias vinculadas a ela.
        builder.HasIndex(p => p.PecaId);
    }
}
