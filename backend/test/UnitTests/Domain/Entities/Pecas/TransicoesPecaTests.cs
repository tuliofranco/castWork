using Castwork.Domain.Entities.Pecas;

namespace Castwork.UnitTests.Domain.Entities.Pecas;

public class TransicoesPecaTests
{
    [Test]
    public void DeveTerTransicoesDefinidasParaTodoStatus([Values] StatusPeca status)
    {
        var obter = () => TransicoesPeca.ProximosDe(status);

        obter.Should().NotThrow();
    }

    [TestCase(StatusPeca.Briefing, new[] { StatusPeca.Orcamento, StatusPeca.Cancelada })]
    [TestCase(StatusPeca.Acabamento, new[] { StatusPeca.Entregue, StatusPeca.Producao, StatusPeca.Cancelada })]
    [TestCase(StatusPeca.Entregue, new[] { StatusPeca.PosVenda })]
    [TestCase(StatusPeca.Cancelada, new StatusPeca[0])]
    public void DeveInformarProximosStatusPossiveis(StatusPeca status, StatusPeca[] esperados)
    {
        var proximos = TransicoesPeca.ProximosDe(status);

        proximos.Should().BeEquivalentTo(esperados);
    }
}
