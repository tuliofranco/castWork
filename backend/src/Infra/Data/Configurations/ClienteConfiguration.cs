using Castwork.Domain.Entities.Clientes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(c => c.Nome).HasMaxLength(Cliente.NomeMaxLength).IsRequired();
        builder.Property(c => c.Documento);
        builder.Property(c => c.Email).HasMaxLength(Cliente.EmailMaxLength);
        builder.Property(c => c.Telefone).HasMaxLength(Cliente.TelefoneMaxLength);
        builder.Property(c => c.Observacoes).HasMaxLength(Cliente.ObservacoesMaxLength);
        builder.Property(c => c.Ativo).IsRequired();

        // Complex type: as colunas ficam na própria tabela (endereco_cep, endereco_uf...), sem join nem Id.
        builder.ComplexProperty(c => c.Endereco, e => e.ConfigurarEndereco());

        builder.HasIndex(c => c.Nome);
        builder.HasIndex(c => c.Documento).IsUnique().HasFilter("documento IS NOT NULL");
    }
}
