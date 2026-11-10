using Castwork.Domain.Entities.Precificacao;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Precificacao;

/// <summary>
/// Histórico de cotações: só INSERT. A vigente de cada metal é a de <c>registrada_em</c> mais recente (o índice
/// cobre essa leitura e a do histórico paginado).
/// </summary>
internal sealed class CotacaoMetalConfiguration : IEntityTypeConfiguration<CotacaoMetal>
{
    public void Configure(EntityTypeBuilder<CotacaoMetal> builder)
    {
        builder.ToTable("cotacoes_metal");
        builder.HasKey(c => c.Id);
        builder.IgnorarEventos();

        builder.Property(c => c.Metal).IsRequired();
        builder.Property(c => c.Valor).IsRequired();
        builder.Property(c => c.RegistradaEm).IsRequired();
        builder.Property(c => c.RegistradaPor);

        builder.HasIndex(c => new { c.Metal, c.RegistradaEm }).IsDescending(false, true);
    }
}
