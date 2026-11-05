using Castwork.Application.Common.Security;
using Castwork.GraphQl;
using Castwork.TestBootstrap.Ambiente;

using HotChocolate.Execution;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.IntegrationTests.GraphQl;

/// <summary>
/// Contrato do schema com o front. Mudou o schema de propósito? Rode com ATUALIZAR_SNAPSHOT=1
/// e revise o diff do schema.graphql no PR: é a quebra de contrato ficando visível.
/// </summary>
public class SchemaGraphQlTests
{
    private static readonly string CaminhoDoSnapshot = Path.Combine(RaizDaSolucao(), "test", "IntegrationTests", "GraphQl", "schema.graphql");

    [Test]
    public async Task DeveManterOSchemaIgualAoSnapshotAsync()
    {
        var schema = (await SchemaAsync()).ToString().ReplaceLineEndings("\n").Trim() + "\n";

        if (Environment.GetEnvironmentVariable("ATUALIZAR_SNAPSHOT") == "1" || !File.Exists(CaminhoDoSnapshot))
        {
            await File.WriteAllTextAsync(CaminhoDoSnapshot, schema);
            Assert.Inconclusive($"Snapshot gravado em {CaminhoDoSnapshot}. Revise e rode de novo.");
        }

        schema.Should().Be((await File.ReadAllTextAsync(CaminhoDoSnapshot)).ReplaceLineEndings("\n"), "o schema GraphQL mudou: atualize o snapshot de propósito ou desfaça a mudança");
    }

    [Test]
    public async Task DeveExigirAutorizacaoEmTodoCampoDaQueryAsync()
    {
        var schema = await SchemaAsync();

        var semAutorizacao = schema.QueryType.Fields
            .Where(f => !f.IsIntrospectionField)
            .Where(f => !f.Directives.ContainsName("authorize"))
            .Select(f => f.Name);

        semAutorizacao.Should().BeEmpty("todo resolver precisa de [Authorize]: o endpoint /graphql é anônimo");
    }

    [Test]
    public void DeveTodaPoliticaDoGraphQlExistirNoCatalogoDeRegras()
    {
        var politicas = typeof(Politicas).GetFields().Select(f => (string)f.GetRawConstantValue()!);

        CatalogoDeRegras.Todas.Select(r => r.Codigo).Should().Contain(politicas);
    }

    private static string RaizDaSolucao()
    {
        var pasta = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Castwork.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("Castwork.slnx não encontrado acima da pasta de testes.");
    }

    private static async Task<HotChocolate.ISchemaDefinition> SchemaAsync()
        => (await AmbienteDeTeste.Obter<IServiceProvider>().GetRequestExecutorAsync()).Schema;
}
