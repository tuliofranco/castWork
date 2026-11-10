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

        // Preços do projeto: cópia dos do ateliê, editável só aqui. Sem default nas colunas: o valor vem sempre do domínio
        // (na criação) ou da migration (projetos que já existiam).
        builder.ComplexProperty(p => p.ParametrosDePreco, p =>
        {
            p.Property(x => x.PerdaPercentual).HasColumnName("perda_percentual").HasPrecision(5, 2);
            p.Property(x => x.ValorHora).HasColumnName("valor_hora");
            p.Property(x => x.Markup).HasColumnName("markup").HasPrecision(5, 2);
            p.Property(x => x.TaxasPercentual).HasColumnName("taxas_percentual").HasPrecision(5, 2);
            p.Property(x => x.ArredondarPara).HasColumnName("arredondar_para");
        });
        builder.Property(p => p.PrecosHerdadosEm).IsRequired();
        builder.Property(p => p.PrecosAlteradosEm);

        builder.HasMany(p => p.Cotacoes).WithOne().HasForeignKey("ProjetoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Cotacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Versoes).WithOne().HasForeignKey("ProjetoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Versoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.Nome);
    }
}
