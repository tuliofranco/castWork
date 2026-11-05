using Castwork.Domain.Entities.Pecas;

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
        builder.Property(p => p.Peso);
        builder.Property(p => p.Descricao).HasMaxLength(Peca.DescricaoMaxLength);
        builder.Property(p => p.Ativo).IsRequired();

        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.Tipo);
    }
}
