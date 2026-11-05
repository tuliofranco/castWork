using Castwork.TestBootstrap.Ambiente;

namespace Castwork.IntegrationTests;

/// <summary>
/// Sobe o PostgreSQL em container e a API uma única vez para todo o assembly de testes.
/// Requer o Docker rodando.
/// </summary>
[SetUpFixture]
public class ConfiguracaoDoAmbiente
{
    [OneTimeSetUp]
    public Task IniciarAsync() => AmbienteDeTeste.IniciarAsync();

    [OneTimeTearDown]
    public Task EncerrarAsync() => AmbienteDeTeste.EncerrarAsync();
}
