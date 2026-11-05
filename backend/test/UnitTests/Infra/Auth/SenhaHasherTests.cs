using Castwork.Application.Abstractions;
using Castwork.Infra.Auth.Senhas;

namespace Castwork.UnitTests.Infra.Auth;

public class SenhaHasherTests
{
    [Test]
    public void DeveValidarSomenteASenhaQueGerouOHash()
    {
        var hasher = new SenhaHasher();
        var hash = hasher.Hash("correta-123");

        var resultado = new { Certa = hasher.Verificar(hash, "correta-123"), Errada = hasher.Verificar(hash, "Correta-123") };

        resultado.Should().BeEquivalentTo(new { Certa = ResultadoVerificacaoSenha.Valida, Errada = ResultadoVerificacaoSenha.Invalida });
    }

    [Test]
    public void DeveGerarHashesDiferentesParaAMesmaSenhaSemExporOTexto()
    {
        var hasher = new SenhaHasher();

        var hashes = new[] { hasher.Hash("mesma-senha"), hasher.Hash("mesma-senha") };

        hashes.Should().OnlyHaveUniqueItems("cada hash tem salt próprio");
        hashes.Should().AllSatisfy(h => h.Should().NotContain("mesma-senha"));
    }
}
