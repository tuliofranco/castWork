using System.Text.Json;

using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Projetos;

internal sealed class VersaoProjetoConfiguration : IEntityTypeConfiguration<VersaoProjeto>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<VersaoProjeto> builder)
    {
        builder.ToTable("projeto_versoes");
        builder.HasKey(v => v.Id);
        builder.IgnorarEventos();

        builder.Property<long>("ProjetoId");
        builder.Property(v => v.Nome).HasMaxLength(VersaoProjeto.NomeMaxLength).IsRequired();
        builder.Property(v => v.Observacoes).HasMaxLength(VersaoProjeto.ObservacoesMaxLength);
        builder.Property(v => v.Status).IsRequired();

        // Orçamento da versão. Metais e serviços são listas curtas lidas sempre com a versão: jsonb basta. A leitura
        // passa pelos construtores do domínio, então um dado inválido no banco falha ao carregar.
        builder.Property(v => v.Metais)
            .HasColumnType("jsonb")
            .HasConversion(metais => MetaisParaJson(metais), json => MetaisDoJson(json), ComparadorDeLista<MetalDaVersao>())
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.Property(v => v.Servicos)
            .HasColumnType("jsonb")
            .HasConversion(servicos => ServicosParaJson(servicos), json => ServicosDoJson(json), ComparadorDeLista<ServicoExterno>())
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();
        builder.ComplexProperty(v => v.MaoDeObra, m =>
        {
            m.Property(x => x.Tipo).HasColumnName("mao_de_obra_tipo");
            m.Property(x => x.Horas).HasColumnName("mao_de_obra_horas").HasPrecision(6, MaoDeObra.HorasCasasDecimais);
            m.Property(x => x.ValorFixo).HasColumnName("mao_de_obra_valor_fixo");
        });
        builder.Property(v => v.Markup).HasPrecision(5, ParametrosDePreco.CasasDecimais);

        builder.HasMany(v => v.Itens).WithOne().HasForeignKey("VersaoId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(v => v.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Garante no banco a numeração sem repetição dentro do projeto.
        builder.HasIndex("ProjetoId", nameof(VersaoProjeto.Numero)).IsUnique();
    }

    private static string MetaisParaJson(IReadOnlyList<MetalDaVersao> metais)
        => JsonSerializer.Serialize(metais.Select(m => new MetalJson(m.LigaId, m.PesoEstimado.Gramas, m.PesoReal?.Gramas)), Json);

    private static List<MetalDaVersao> MetaisDoJson(string json)
        => (JsonSerializer.Deserialize<List<MetalJson>>(json, Json) ?? [])
            .Select(m => new MetalDaVersao(m.LigaId, Peso.EmGramas(m.PesoEstimado), m.PesoReal is decimal real ? Peso.EmGramas(real) : null))
            .ToList();

    private static string ServicosParaJson(IReadOnlyList<ServicoExterno> servicos)
        => JsonSerializer.Serialize(servicos.Select(s => new ServicoJson(s.Nome, s.Valor.Valor)), Json);

    private static List<ServicoExterno> ServicosDoJson(string json)
        => (JsonSerializer.Deserialize<List<ServicoJson>>(json, Json) ?? [])
            .Select(s => new ServicoExterno(s.Nome, Dinheiro.Criar(s.Valor)))
            .ToList();

    /// <summary>
    /// Listas imutáveis de value objects: iguais quando têm os mesmos itens na mesma ordem.
    /// </summary>
    private static ValueComparer<IReadOnlyList<T>> ComparadorDeLista<T>()
        => new(
            (a, b) => a!.SequenceEqual(b!),
            lista => lista.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            lista => lista.ToList());

    private sealed record MetalJson(long LigaId, decimal PesoEstimado, decimal? PesoReal);

    private sealed record ServicoJson(string Nome, decimal Valor);
}
