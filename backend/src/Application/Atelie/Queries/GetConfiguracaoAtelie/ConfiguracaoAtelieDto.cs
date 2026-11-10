using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Mapping;
using Castwork.Domain.Entities.Atelie;

namespace Castwork.Application.Atelie.Queries.GetConfiguracaoAtelie;

public sealed class ConfiguracaoAtelieDto : IMapFrom<ConfiguracaoAtelie>
{
    public string Nome { get; init; } = string.Empty;

    /// <summary>
    /// CPF/CNPJ com máscara.
    /// </summary>
    public string? Documento { get; init; }

    public string? Email { get; init; }

    public string? Telefone { get; init; }

    public EnderecoDto? Endereco { get; init; }

    public string? Assinatura { get; init; }

    public string CorMarca { get; init; } = string.Empty;

    public string? Instagram { get; init; }

    public string? Site { get; init; }

    public string? AvisoBriefing { get; init; }

    public bool PossuiLogo { get; init; }

    /// <summary>
    /// Value objects viram tipos simples para o front. Roda na projeção final, depois de o EF ler as colunas.
    /// </summary>
    public void Mapping(Mapster.TypeAdapterConfig config)
        => config.NewConfig<ConfiguracaoAtelie, ConfiguracaoAtelieDto>()
            .Map(d => d.Documento, s => s.Documento == null ? null : s.Documento.Formatado)
            .Map(d => d.PossuiLogo, s => s.LogoTipo != null);
}
