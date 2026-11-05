using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// Endereço brasileiro. É um value object: trocar o endereço é substituí-lo inteiro, não editar campo a campo.
/// </summary>
public sealed record Endereco
{
    public const int LogradouroMaxLength = 150;
    public const int NumeroMaxLength = 20;
    public const int ComplementoMaxLength = 100;
    public const int BairroMaxLength = 100;
    public const int CidadeMaxLength = 100;

    private static readonly HashSet<string> Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];

    private Endereco(string cep, string logradouro, string numero, string? complemento, string bairro, string cidade, string uf)
    {
        Cep = cep;
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Uf = uf;
    }

    public string Cep { get; }

    public string Logradouro { get; }

    public string Numero { get; }

    public string? Complemento { get; }

    public string Bairro { get; }

    public string Cidade { get; }

    public string Uf { get; }

    public static bool EhUfValida(string? uf) => uf is not null && Ufs.Contains(uf.Trim().ToUpperInvariant());

    public static Endereco Criar(
        string cep,
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string cidade,
        string uf)
    {
        var cepNormalizado = new string([.. cep.Where(char.IsAsciiDigit)]);
        if (cepNormalizado.Length != 8)
        {
            throw new ValorInvalidoException(nameof(Cep), "deve ter 8 dígitos");
        }

        var ufNormalizada = uf.Trim().ToUpperInvariant();
        if (!EhUfValida(ufNormalizada))
        {
            throw new ValorInvalidoException(nameof(Uf), "não é uma UF válida");
        }

        return new Endereco(
            cepNormalizado,
            Guard.Obrigatorio(logradouro, nameof(Logradouro), LogradouroMaxLength),
            Guard.Obrigatorio(numero, nameof(Numero), NumeroMaxLength),
            Guard.Opcional(complemento, nameof(Complemento), ComplementoMaxLength),
            Guard.Obrigatorio(bairro, nameof(Bairro), BairroMaxLength),
            Guard.Obrigatorio(cidade, nameof(Cidade), CidadeMaxLength),
            ufNormalizada);
    }
}
