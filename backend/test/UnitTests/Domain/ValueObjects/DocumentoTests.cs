using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class DocumentoTests
{
    [TestCase("529.982.247-25", "52998224725", TipoDocumento.Cpf, "529.982.247-25")]
    [TestCase("11222333000181", "11222333000181", TipoDocumento.Cnpj, "11.222.333/0001-81")]
    [TestCase("12.ABC.345/01DE-35", "12ABC34501DE35", TipoDocumento.Cnpj, "12.ABC.345/01DE-35")]
    [TestCase("12abc34501de35", "12ABC34501DE35", TipoDocumento.Cnpj, "12.ABC.345/01DE-35")]
    public void DeveCriarDocumentoValidoNormalizado(string texto, string numero, TipoDocumento tipo, string formatado)
    {
        var documento = Documento.Criar(texto);

        documento.Should().BeEquivalentTo(new { Numero = numero, Tipo = tipo, Formatado = formatado });
    }

    [TestCase("529.982.247-24")]
    [TestCase("111.111.111-11")]
    [TestCase("11.222.333/0001-82")]
    [TestCase("12.ABC.345/01DE-36")]
    [TestCase("00000000000000")]
    [TestCase("1234")]
    [TestCase("5299822472A")]
    public void DeveRejeitarDocumentoInvalido(string texto)
    {
        var criar = () => Documento.Criar(texto);

        criar.Should().Throw<ValorInvalidoException>();
        Documento.EhValido(texto).Should().BeFalse();
    }
}
