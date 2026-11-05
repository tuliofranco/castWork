using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests;

/// <summary>
/// Base dos testes de integração. Antes de cada teste o Respawn zera o banco; depois, cada builder
/// registrado com <see cref="Usar{TBuilder}"/> remove o que criou.
/// </summary>
public abstract class TesteDeIntegracao
{
    private readonly List<IDataBuilder> _builders = [];

    [SetUp]
    public Task ResetarAmbienteAsync() => AmbienteDeTeste.ResetarAsync();

    [TearDown]
    public async Task TearDownBuildersAsync()
    {
        foreach (var builder in Enumerable.Reverse(_builders))
        {
            await builder.TearDownAsync();
        }

        _builders.Clear();
    }

    protected TBuilder Usar<TBuilder>(TBuilder builder)
        where TBuilder : IDataBuilder
    {
        _builders.Add(builder);
        return builder;
    }
}
