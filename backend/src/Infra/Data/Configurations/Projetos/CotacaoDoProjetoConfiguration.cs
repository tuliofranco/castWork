using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class CotacaoDoProjetoConfiguration : IEntityTypeConfiguration<CotacaoDoProjeto>
{
    public void Configure(EntityTypeBuilder<CotacaoDoProjeto> builder)
    {
        builder.ToTable("projeto_cotacoes");
        builder.HasKey(c => c.Id);
        builder.IgnorarEventos();

        builder.Property<long>("ProjetoId");
        builder.Property(c => c.Metal).IsRequired();
        builder.Property(c => c.Valor).IsRequired();
        builder.Property(c => c.CotadaEm).IsRequired();

        // Uma cotação por metal em cada projeto (regra do domínio).
        builder.HasIndex("ProjetoId", nameof(CotacaoDoProjeto.Metal)).IsUnique();
    }
}
