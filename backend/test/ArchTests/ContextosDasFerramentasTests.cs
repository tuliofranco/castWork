using System.Reflection;

using NetArchTest.Rules;

namespace Castwork.ArchTests;

/// <summary>
/// Cada ferramenta é um contexto próprio e o Assistente é genérico: o Assistente não conhece nenhuma
/// ferramenta pelo nome, e uma ferramenta não conhece outra. Vale em todas as camadas.
/// </summary>
public class ContextosDasFerramentasTests
{
    private static readonly Assembly[] AssembliesComContextos =
    [
        Camadas.DomainAssembly,
        Camadas.ApplicationAssembly,
        typeof(Infra.IA.AssemblyReference).Assembly,
        Camadas.WebApiAssembly,
    ];

    [TestCaseSource(nameof(AssembliesComContextos))]
    public void DeveAssistenteNaoDependerDeNenhumaFerramenta(Assembly assembly)
    {
        foreach (var raiz in Raizes(assembly))
        {
            var resultado = Types.InAssembly(assembly)
                .That().ResideInNamespace($"{raiz}.Assistente")
                .ShouldNot().HaveDependencyOnAny([.. Ferramentas(assembly).Select(f => $"{raiz}.Ferramentas.{f}")])
                .GetResult();

            Assert.That(resultado.IsSuccessful, Is.True, Falhas(resultado));
        }
    }

    [TestCaseSource(nameof(AssembliesComContextos))]
    public void DeveFerramentaNaoDependerDeOutraFerramenta(Assembly assembly)
    {
        var ferramentas = Ferramentas(assembly);
        foreach (var raiz in Raizes(assembly))
        {
            foreach (var ferramenta in ferramentas)
            {
                var outras = ferramentas.Where(f => f != ferramenta).Select(f => $"{raiz}.Ferramentas.{f}").ToArray();
                if (outras.Length == 0)
                {
                    continue;
                }

                var resultado = Types.InAssembly(assembly)
                    .That().ResideInNamespace($"{raiz}.Ferramentas.{ferramenta}")
                    .ShouldNot().HaveDependencyOnAny(outras)
                    .GetResult();

                Assert.That(resultado.IsSuccessful, Is.True, Falhas(resultado));
            }
        }
    }

    /// <summary>Namespaces logo acima de <c>.Assistente</c>/<c>.Ferramentas</c> (ex.: Castwork.Domain.Entities).</summary>
    private static string[] Raizes(Assembly assembly)
        => [.. assembly.GetTypes()
            .Select(t => t.Namespace ?? string.Empty)
            .Select(n => n.IndexOf(".Ferramentas.", StringComparison.Ordinal) is var i and >= 0 ? n[..i] : null)
            .OfType<string>()
            .Distinct()];

    private static string[] Ferramentas(Assembly assembly)
        => [.. assembly.GetTypes()
            .Select(t => t.Namespace ?? string.Empty)
            .Select(n => n.Split(".Ferramentas.", 2))
            .Where(p => p.Length == 2)
            .Select(p => p[1].Split('.')[0])
            .Distinct()];

    private static string Falhas(TestResult resultado)
        => string.Join(", ", resultado.FailingTypeNames ?? []);
}
