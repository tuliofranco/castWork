using System.Reflection;

using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.ArchTests;

/// <summary>
/// "Negar por padrão" no MediatR: todo request precisa declarar como é autorizado.
/// Esquecer o atributo quebra o build, em vez de virar um endpoint aberto.
/// </summary>
public class RequestsTests
{
    private static readonly Type[] AtributosDeSeguranca =
        [typeof(PermissaoAttribute), typeof(SomenteAutenticadoAttribute), typeof(SemPermissaoAttribute)];

    [Test]
    public void DeveTodoRequestDeclararExatamenteUmAtributoDeSeguranca()
    {
        var violacoes = Requests()
            .Select(t => (Tipo: t, Quantidade: AtributosDeSeguranca.Count(a => t.IsDefined(a, inherit: false))))
            .Where(x => x.Quantidade != 1)
            .Select(x => $"{x.Tipo.FullName} ({x.Quantidade} atributos)");

        violacoes.Should().BeEmpty("use [Permissao(Regras.X.Y)], [SomenteAutenticado] ou [SemPermissao]");
    }

    [Test]
    public void DeveExistirAoMenosUmRequestNaApplication()
    {
        var requests = Requests();

        requests.Should().NotBeEmpty("senão o teste acima passaria sem verificar nada");
    }

    [Test]
    public void DeveTodaRegraTerGuidValidoUnicoCodigoEDescricao()
    {
        var regras = typeof(Regras).GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral)
            .Select(f => new
            {
                Nome = $"{f.DeclaringType!.Name}.{f.Name}",
                Valor = (string)f.GetRawConstantValue()!,
                Codigo = f.GetCustomAttribute<Regras.RegraAttribute>()?.Codigo,
                Descricao = f.GetCustomAttribute<Regras.RegraAttribute>()?.Descricao,
            })
            .ToList();

        regras.Should().NotBeEmpty();
        regras.Should().AllSatisfy(r =>
        {
            Guid.TryParse(r.Valor, out _).Should().BeTrue($"{r.Nome} precisa ser um GUID");
            r.Descricao.Should().NotBeNullOrWhiteSpace($"{r.Nome} precisa de [Regra]");
            r.Codigo.Should().MatchRegex("^[a-z]+\\.[a-z]+$", $"{r.Nome}: código no formato <dominio>.<acao>");
        });
        regras.Select(r => Guid.Parse(r.Valor)).Should().OnlyHaveUniqueItems();
        regras.Select(r => r.Codigo).Should().OnlyHaveUniqueItems();
    }

    private static List<Type> Requests()
        => [.. Camadas.ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i == typeof(IBaseRequest)))];
}
