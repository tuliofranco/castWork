using Castwork.Domain.Entities.Assistente;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Ferramentas;

internal sealed class MensagemFerramentaConfiguration : IEntityTypeConfiguration<MensagemFerramenta>
{
    public void Configure(EntityTypeBuilder<MensagemFerramenta> builder)
    {
        builder.ToTable("ferramenta_mensagens");
        builder.HasKey(m => m.Id);
        builder.IgnorarEventos();

        builder.Property<long>("ConversaId");
        builder.Property(m => m.Papel).IsRequired();
        builder.Property(m => m.Chat).IsRequired();
        builder.Property(m => m.Texto);
        builder.Property(m => m.Conteudo).HasColumnType("jsonb").IsRequired();
        builder.Property(m => m.Imagens).IsRequired();
        builder.Property(m => m.CriadaEm).IsRequired();
        builder.Property(m => m.Pensamento);
        builder.Property(m => m.Passos).IsRequired();
        builder.Property(m => m.Tokens);

        builder.OwnsOne(m => m.Acao, acao =>
        {
            acao.Property(a => a.Descricao).HasColumnName("acao_descricao").HasMaxLength(AcaoDaIa.DescricaoMaxLength);
            acao.Property(a => a.ArquivoAntesId).HasColumnName("acao_arquivo_antes_id");
        });

        builder.HasIndex("ConversaId", nameof(MensagemFerramenta.Ordem)).IsUnique();
    }
}
