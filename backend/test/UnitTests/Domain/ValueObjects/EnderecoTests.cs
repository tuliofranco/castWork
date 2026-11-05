using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.UnitTests.Domain.ValueObjects;

public class EnderecoTests
{
    [Test]
    public void DeveCriarEnderecoNormalizandoCepEUf()
    {
        var endereco = Endereco.Criar("80.010-000", "Rua XV de Novembro", "100", " ", "Centro", "Curitiba", "pr");

        endereco.Should().BeEquivalentTo(new
        {
            Cep = "80010000",
            Logradouro = "Rua XV de Novembro",
            Numero = "100",
            Complemento = (string?)null,
            Bairro = "Centro",
            Cidade = "Curitiba",
            Uf = "PR",
        });
    }

    [TestCase("8001000", "PR", "Cep")]
    [TestCase("80010000", "XX", "Uf")]
    public void DeveRejeitarEnderecoInvalido(string cep, string uf, string campo)
    {
        var criar = () => Endereco.Criar(cep, "Rua", "1", null, "Centro", "Curitiba", uf);

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(campo);
    }
}
