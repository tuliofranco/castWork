using MediatR;

namespace Castwork.ArchTests;

/// <summary>
/// Trava contra upgrade acidental para versões comerciais (Lucky Penny): MediatR 13+ e AutoMapper 15+ são pagos.
/// </summary>
public class LicencasTests
{
    [Test]
    public void DeveMediatRPermanecerNaVersao12Gratuita()
    {
        var versao = typeof(IMediator).Assembly.GetName().Version!;

        versao.Major.Should().Be(12, "da 13 em diante o MediatR exige licença comercial");
    }

    [Test]
    public void DeveNaoHaverAutoMapperEmNenhumAssemblyDaSolucao()
    {
        var referencias = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Castwork.", StringComparison.Ordinal) == true)
            .SelectMany(a => a.GetReferencedAssemblies())
            .Select(r => r.Name);

        referencias.Should().NotContain("AutoMapper", "o mapeamento é feito com Mapster (MIT)");
    }
}
