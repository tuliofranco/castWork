using Castwork.Domain.ValueObjects;

namespace Castwork.Application.Common.Enderecos;

/// <summary>
/// Endereço como chega do front. Vira o value object <see cref="Endereco"/> no handler.
/// </summary>
public sealed class EnderecoDto
{
    public string Cep { get; set; } = string.Empty;

    public string Logradouro { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string? Complemento { get; set; }

    public string Bairro { get; set; } = string.Empty;

    public string Cidade { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;

    public Endereco ParaValueObject() => Endereco.Criar(Cep, Logradouro, Numero, Complemento, Bairro, Cidade, Uf);
}
