using Castwork.Domain.Entities.Acesso;

namespace Castwork.UnitTests.Domain.Entities.Acesso;

public class PerfilTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    [Test]
    public void DeveCriarPerfilSemRegrasDuplicadas()
    {
        var perfil = Perfil.Criar("Bancada", [A, B, A]);

        perfil.Regras.Select(r => r.RegraId).Should().BeEquivalentTo([A, B]);
    }

    [Test]
    public void DeveRedefinirRegrasMantendoAsQueContinuam()
    {
        var perfil = Perfil.Criar("Bancada", [A, B]);
        var vinculoDeB = perfil.Regras.Single(r => r.RegraId == B);

        perfil.DefinirRegras([B, C]);

        perfil.Regras.Select(r => r.RegraId).Should().BeEquivalentTo([B, C]);
        perfil.Regras.Single(r => r.RegraId == B).Should().BeSameAs(vinculoDeB, "a linha da regra que continua não é recriada");
    }
}
