using System.Reflection;

using NetArchTest.Rules;

namespace Castwork.ArchTests;

/// <summary>
/// Garante a regra Presentation → Hosting → Infra → Application → Domain.
/// Cada teste quebra se uma camada interna passar a depender de uma externa.
/// </summary>
public class RegraDeDependenciaTests
{
    [Test]
    public void DeveDomainNaoDependerDeNenhumaOutraCamada()
        => AssertNaoDepende(
            Camadas.DomainAssembly,
            Camadas.DomainService,
            Camadas.Application,
            Camadas.Infra,
            Camadas.GraphQl,
            Camadas.WebApi);

    [Test]
    public void DeveDomainNaoDependerDeBibliotecasExternas()
        => AssertNaoDepende(Camadas.DomainAssembly, [.. Camadas.FrameworksDeInfra, "MediatR", "AutoMapper", "FluentValidation"]);

    [Test]
    public void DeveDomainServiceDependerApenasDoDomain()
        => AssertNaoDepende(
            Camadas.DomainServiceAssembly,
            [Camadas.Application, Camadas.Infra, Camadas.GraphQl, Camadas.WebApi, .. Camadas.FrameworksDeInfra]);

    [Test]
    public void DeveApplicationNaoDependerDeInfraNemPresentation()
        => AssertNaoDepende(
            Camadas.ApplicationAssembly,
            [Camadas.Infra, Camadas.GraphQl, Camadas.WebApi, .. Camadas.FrameworksDeInfraExcetoEfCore, .. Camadas.PartesProibidasDoEfCore]);

    /// <summary>
    /// A Application usa as extensões de consulta do EF Core (ToListAsync, AnyAsync) de propósito,
    /// mas nunca o DbContext/DbSet nem o provider: o acesso continua sendo pelo IUnitOfWork.
    /// </summary>
    [Test]
    public void DeveApplicationCommonUsarSoExtensoesDeConsultaDoEfCore()
        => AssertNaoDepende(
            Camadas.ApplicationCommonAssembly,
            [.. Camadas.FrameworksDeInfraExcetoEfCore, .. Camadas.PartesProibidasDoEfCore]);

    [Test]
    public void DeveApplicationCommonNaoDependerDeOutrasCamadas()
        => AssertNaoDepende(
            Camadas.ApplicationCommonAssembly,
            Camadas.Domain,
            Camadas.Infra,
            Camadas.GraphQl,
            Camadas.WebApi);

    [TestCaseSource(nameof(InfraAssemblies))]
    public void DeveInfraNaoDependerDePresentationNemDoHosting(Assembly assembly)
        => AssertNaoDepende(assembly, Camadas.Hosting, Camadas.GraphQl, Camadas.WebApi);

    [Test]
    public void DeveGraphQlNaoDependerDaWebApi()
        => AssertNaoDepende(Camadas.GraphQlAssembly, Camadas.WebApi);

    [Test]
    public void DevePresentationNaoAcessarInfraDiretamente()
    {
        // Presentation só conhece a Infra pelo Hosting (registro de DI) e pelas abstrações da Application.
        var infraConcreta = new[] { "Castwork.Infra.Data", "Castwork.Infra.Auth", "Castwork.Infra.MediatR", "Castwork.Infra.FluentValidation" };

        AssertNaoDepende(Camadas.WebApiAssembly, infraConcreta);
        AssertNaoDepende(Camadas.GraphQlAssembly, infraConcreta);
    }

    private static Assembly[] InfraAssemblies() => Camadas.InfraAssemblies;

    private static void AssertNaoDepende(Assembly assembly, params string[] proibidos)
    {
        var resultado = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(proibidos)
            .GetResult();

        resultado.IsSuccessful.Should().BeTrue(
            "{0} não pode depender de [{1}], mas os tipos {2} dependem",
            assembly.GetName().Name,
            string.Join(", ", proibidos),
            string.Join(", ", resultado.FailingTypeNames ?? []));
    }
}
