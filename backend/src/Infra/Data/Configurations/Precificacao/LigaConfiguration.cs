using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Castwork.Infra.Data.Configurations.Precificacao;

internal sealed class LigaConfiguration : IEntityTypeConfiguration<Liga>
{
    /// <summary>
    /// As ligas pré-cadastradas ocupam Ids fixos; as criadas pelo ateliê começam daqui, sem colidir com o seed.
    /// </summary>
    public const long PrimeiroIdDoAtelie = 1000;

    private static readonly DateTimeOffset CriadoNoSeed = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Liga> builder)
    {
        builder.ToTable("ligas");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasIdentityOptions(startValue: PrimeiroIdDoAtelie);
        builder.IgnorarEventos().ConfigurarAuditoria().ConfigurarConcorrencia();

        builder.Property(l => l.Nome).HasMaxLength(Liga.NomeMaxLength).IsRequired();
        builder.Property(l => l.Metal).IsRequired();
        builder.Property(l => l.Teor).IsRequired();
        builder.Property(l => l.Densidade).HasPrecision(6, Liga.DensidadeCasasDecimais).IsRequired();
        builder.Property(l => l.Ativo).IsRequired();

        builder.HasIndex(l => l.Nome);

        builder.HasData(
            Seed(1, "Ouro 24k", Metal.Ouro, 999, 19.3m),
            Seed(2, "Ouro 18k amarelo", Metal.Ouro, 750, 15.5m),
            Seed(3, "Ouro 18k branco", Metal.Ouro, 750, 15.0m),
            Seed(4, "Ouro 18k rosé", Metal.Ouro, 750, 15.1m),
            Seed(5, "Ouro 14k amarelo", Metal.Ouro, 585, 13.4m),
            Seed(6, "Prata 950", Metal.Prata, 950, 10.4m),
            Seed(7, "Prata 925", Metal.Prata, 925, 10.36m));
    }

    private static object Seed(long id, string nome, Metal metal, int teor, decimal densidade)
        => new { Id = id, Nome = nome, Metal = metal, Teor = TeorMetal.EmMilesimos(teor), Densidade = densidade, Ativo = true, CriadoEm = CriadoNoSeed };
}
