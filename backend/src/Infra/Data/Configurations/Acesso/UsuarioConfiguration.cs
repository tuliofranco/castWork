using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Acesso;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(u => u.Nome).HasMaxLength(Usuario.NomeMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(Usuario.EmailMaxLength).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(Usuario.SenhaHashMaxLength).IsRequired();
        builder.Property(u => u.VersaoSeguranca).IsRequired();

        builder.HasOne<Perfil>().WithMany().HasForeignKey(u => u.PerfilId).OnDelete(DeleteBehavior.Restrict);

        // O e-mail já é gravado normalizado (minúsculo), então o índice único comum basta.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
