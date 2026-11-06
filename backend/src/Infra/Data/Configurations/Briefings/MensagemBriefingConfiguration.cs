using Castwork.Domain.Entities.Briefings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Briefings;

internal sealed class MensagemBriefingConfiguration : IEntityTypeConfiguration<MensagemBriefing>
{
    public void Configure(EntityTypeBuilder<MensagemBriefing> builder)
    {
        builder.ToTable("briefing_mensagens");
        builder.HasKey(m => m.Id);
        builder.IgnorarEventos();

        builder.Property<long>("BriefingId");
        builder.Property(m => m.Papel).IsRequired();
        builder.Property(m => m.Texto);

        // Mensagem completa da API (blocos, ferramentas), guardada como JSON para retomar a conversa.
        builder.Property(m => m.Conteudo).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.CriadaEm).IsRequired();

        // Chaves das imagens anexadas (uuid[]): o arquivo fica em briefing_imagens.
        builder.Property(m => m.Imagens).IsRequired();

        builder.HasIndex("BriefingId", nameof(MensagemBriefing.Ordem)).IsUnique();
    }
}
