using Castwork.Domain.Entities.Assistente;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ia;

internal sealed class ConsumoDaIaConfiguration : IEntityTypeConfiguration<ConsumoDaIa>
{
    public void Configure(EntityTypeBuilder<ConsumoDaIa> builder)
    {
        builder.ToTable("ia_consumo");
        builder.HasKey(c => c.Id);
        builder.IgnorarEventos();

        // Sem chave estrangeira: o registro de consumo fica mesmo se a conversa for apagada (é histórico de custo).
        builder.Property(c => c.Origem).HasMaxLength(ConsumoDaIa.OrigemMaxLength).IsRequired();
        builder.Property(c => c.Modelo).HasMaxLength(ConsumoDaIa.ModeloMaxLength).IsRequired();
        builder.Ignore(c => c.Total);
        builder.HasIndex(c => new { c.Origem, c.ConversaId });
        builder.HasIndex(c => c.RegistradoEm);
    }
}
