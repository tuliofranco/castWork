using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Eventos;
using Castwork.Domain.Exceptions;

using NetArchTest.Rules;

namespace Castwork.ArchTests;

/// <summary>
/// Convenções do Domain que o compilador não garante sozinho.
/// </summary>
public class ConvencoesDoDominioTests
{
    [Test]
    public void DeveEventosDeDominioTerminarEmEventESeremImutaveis()
    {
        var resultado = Types.InAssembly(Camadas.DomainAssembly)
            .That().ImplementInterface(typeof(IDomainEvent)).And().AreNotAbstract()
            .Should().HaveNameEndingWith("Event").And().BeSealed()
            .GetResult();

        resultado.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Test]
    public void DeveExcecoesDoDominioHerdarDeDomainException()
    {
        var resultado = Types.InAssembly(Camadas.DomainAssembly)
            .That().Inherit(typeof(Exception)).And().DoNotHaveName(nameof(DomainException))
            .Should().Inherit(typeof(DomainException)).And().HaveNameEndingWith("Exception")
            .GetResult();

        resultado.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Test]
    public void DeveValueObjectsSeremSelados()
    {
        var resultado = Types.InAssembly(Camadas.DomainAssembly)
            .That().ResideInNamespace("Castwork.Domain.ValueObjects").And().AreClasses()
            .Should().BeSealed()
            .GetResult();

        resultado.FailingTypeNames.Should().BeNullOrEmpty();
    }

    [Test]
    public void DeveEntidadesNaoTerSettersPublicos()
    {
        var violacoes = TiposDoDominio()
            .Where(t => EhEntidade(t) || t.Namespace == "Castwork.Domain.ValueObjects")
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}");

        violacoes.Should().BeEmpty("o estado só muda pelos métodos de negócio da entidade");
    }

    [Test]
    public void DeveAgregadosSeremEntidades()
    {
        var naoEntidades = TiposDoDominio()
            .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t) && !t.IsInterface && !EhEntidade(t))
            .Select(t => t.Name);

        naoEntidades.Should().BeEmpty();
    }

    private static Type[] TiposDoDominio() => Camadas.DomainAssembly.GetTypes();

    private static bool EhEntidade(Type tipo)
    {
        for (var atual = tipo.BaseType; atual is not null; atual = atual.BaseType)
        {
            if (atual.IsGenericType && atual.GetGenericTypeDefinition() == typeof(BaseEntity<>))
            {
                return true;
            }
        }

        return false;
    }
}
