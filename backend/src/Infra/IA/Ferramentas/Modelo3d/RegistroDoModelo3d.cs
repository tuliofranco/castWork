using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// O que o modelo 3D precisa além da definição: o caminho do SolidCraft e a checagem dele ao subir a API.
/// </summary>
internal static class RegistroDoModelo3d
{
    public static IServiceCollection AddModelo3d(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SolidCraftOptions>().Bind(configuration.GetSection(SolidCraftOptions.Secao));
        services.AddHostedService<VerificacaoSolidCraft>();
        return services;
    }
}
