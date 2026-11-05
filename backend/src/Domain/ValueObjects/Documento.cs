using Castwork.Domain.Exceptions;

namespace Castwork.Domain.ValueObjects;

/// <summary>
/// CPF ou CNPJ validado pelos dígitos verificadores e guardado sem máscara.
/// Suporta o CNPJ alfanumérico (Receita Federal, a partir de jul/2026): os 12 primeiros caracteres
/// podem ser letras, e o cálculo do DV usa o valor ASCII − 48 de cada caractere, o que mantém os CNPJs numéricos válidos.
/// </summary>
public sealed record Documento
{
    private static readonly int[] PesosCpf1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCpf2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCnpj1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosCnpj2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Documento(string numero, TipoDocumento tipo)
    {
        Numero = numero;
        Tipo = tipo;
    }

    public string Numero { get; }

    public TipoDocumento Tipo { get; }

    public string Formatado => Tipo == TipoDocumento.Cpf
        ? $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}"
        : $"{Numero[..2]}.{Numero[2..5]}.{Numero[5..8]}/{Numero[8..12]}-{Numero[12..]}";

    public static Documento Criar(string texto)
    {
        var numero = Normalizar(texto);

        return numero.Length switch
        {
            11 when CpfValido(numero) => new Documento(numero, TipoDocumento.Cpf),
            14 when CnpjValido(numero) => new Documento(numero, TipoDocumento.Cnpj),
            _ => throw new ValorInvalidoException(nameof(Documento), "não é um CPF ou CNPJ válido"),
        };
    }

    public static bool EhValido(string texto)
    {
        var numero = Normalizar(texto);
        return (numero.Length == 11 && CpfValido(numero)) || (numero.Length == 14 && CnpjValido(numero));
    }

    public override string ToString() => Formatado;

    private static string Normalizar(string texto)
        => new([.. texto.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);

    private static bool CpfValido(string cpf)
        => cpf.All(char.IsAsciiDigit)
            && cpf.Distinct().Count() > 1
            && cpf[9] - '0' == DigitoVerificador(cpf[..9], PesosCpf1)
            && cpf[10] - '0' == DigitoVerificador(cpf[..10], PesosCpf2);

    private static bool CnpjValido(string cnpj)
        => cnpj[..12].All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c))
            && char.IsAsciiDigit(cnpj[12]) && char.IsAsciiDigit(cnpj[13])
            && cnpj.Distinct().Count() > 1
            && cnpj[12] - '0' == DigitoVerificador(cnpj[..12], PesosCnpj1)
            && cnpj[13] - '0' == DigitoVerificador(cnpj[..13], PesosCnpj2);

    private static int DigitoVerificador(string baseCalculo, int[] pesos)
    {
        var soma = baseCalculo.Select((c, i) => (c - '0') * pesos[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
