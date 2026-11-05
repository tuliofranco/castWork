using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations;

internal static class ConfigurationExtensions
{
    public const string ColunaVersao = "Versao";

    /// <summary>
    /// Colunas de auditoria. Preenchidas pelo <c>AuditoriaInterceptor</c>.
    /// </summary>
    public static EntityTypeBuilder<T> ConfigurarAuditoria<T>(this EntityTypeBuilder<T> builder)
        where T : class, IAuditavel
    {
        builder.Property(e => e.CriadoEm).IsRequired();
        builder.Property(e => e.CriadoPor);
        builder.Property(e => e.AlteradoEm);
        builder.Property(e => e.AlteradoPor);
        return builder;
    }

    /// <summary>
    /// Concorrência otimista pela coluna de sistema <c>xmin</c> do PostgreSQL (muda a cada UPDATE da linha),
    /// mapeada como shadow property: o domínio não sabe que ela existe e não há coluna extra na tabela.
    /// Dois usuários salvando a mesma peça → o segundo recebe <c>DbUpdateConcurrencyException</c> (vira 409).
    /// </summary>
    public static EntityTypeBuilder<T> ConfigurarConcorrencia<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<uint>(ColunaVersao).IsRowVersion();
        return builder;
    }

    /// <summary>
    /// Eventos de domínio e propriedades calculadas não são colunas.
    /// </summary>
    public static EntityTypeBuilder<T> IgnorarEventos<T>(this EntityTypeBuilder<T> builder)
        where T : BaseEntity<long>
    {
        builder.Ignore(e => e.DomainEvents);
        return builder;
    }

    public static ComplexPropertyBuilder<Endereco> ConfigurarEndereco(this ComplexPropertyBuilder<Endereco> endereco)
    {
        endereco.Property(e => e.Cep).HasMaxLength(8).IsFixedLength().IsRequired();
        endereco.Property(e => e.Logradouro).HasMaxLength(Endereco.LogradouroMaxLength).IsRequired();
        endereco.Property(e => e.Numero).HasMaxLength(Endereco.NumeroMaxLength).IsRequired();
        endereco.Property(e => e.Complemento).HasMaxLength(Endereco.ComplementoMaxLength);
        endereco.Property(e => e.Bairro).HasMaxLength(Endereco.BairroMaxLength).IsRequired();
        endereco.Property(e => e.Cidade).HasMaxLength(Endereco.CidadeMaxLength).IsRequired();
        endereco.Property(e => e.Uf).HasMaxLength(2).IsFixedLength().IsRequired();
        return endereco;
    }
}
