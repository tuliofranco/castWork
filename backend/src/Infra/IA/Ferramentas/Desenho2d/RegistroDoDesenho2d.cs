using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA.Ferramentas.Desenho2d;

/// <summary>
/// O que o desenho 2D precisa além da definição: o caminho do CADCraft e a checagem dele ao subir a API.
/// </summary>
internal static class RegistroDoDesenho2d
{
    public static IServiceCollection AddDesenho2d(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CadCraftOptions>().Bind(configuration.GetSection(CadCraftOptions.Secao));
        services.AddHostedService<VerificacaoCadCraft>();
        return services;
    }
}
