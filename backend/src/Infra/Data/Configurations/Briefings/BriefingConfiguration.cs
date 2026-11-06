using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Entities.Projetos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Briefings;

internal sealed class BriefingConfiguration : IEntityTypeConfiguration<Briefing>
{
    public void Configure(EntityTypeBuilder<Briefing> builder)
    {
        builder.ToTable("briefings");
        builder.HasKey(b => b.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        // Um briefing por projeto; some junto com o projeto.
        builder.HasOne<Projeto>().WithOne().HasForeignKey<Briefing>(b => b.ProjetoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(b => b.ProjetoId).IsUnique();

        builder.Property(b => b.TipoJoia).IsRequired();
        builder.Property(b => b.OQueOClienteQuer).HasMaxLength(Briefing.TextoLongoMaxLength).IsRequired();
        builder.Property(b => b.Ocasiao).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Materiais).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Medidas).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Orcamento).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.OQueSeraFeito).HasMaxLength(Briefing.TextoLongoMaxLength);
        builder.Property(b => b.Acabamento).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Pedras).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Fecho).HasMaxLength(Briefing.TextoCurtoMaxLength);
        builder.Property(b => b.Observacoes).HasMaxLength(Briefing.TextoLongoMaxLength);
    }
}
