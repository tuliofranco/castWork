using Castwork.Infra.FluentValidation;

namespace Castwork.UnitTests;

/// <summary>
/// Roda uma vez antes de todos os testes do assembly: aplica as mesmas opções globais da API.
/// </summary>
[SetUpFixture]
public class ConfiguracaoGlobalDosTestes
{
    [OneTimeSetUp]
    public void Configurar() => FluentValidationConfiguration.ConfigurarGlobal();
}
