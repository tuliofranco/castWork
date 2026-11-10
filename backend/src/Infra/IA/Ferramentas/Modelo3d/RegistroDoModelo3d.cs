using Castwork.Application.Ferramentas.Modelo3d;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// O que o modelo 3D precisa além da definição: o caminho do SolidCraft, a checagem dele ao subir a API, o medidor do
/// modelo (peso do metal no preço da versão), que guarda as medições em memória, o editor da conferência de materiais e o
/// montador do modelo da versão a partir das peças.
/// </summary>
internal static class RegistroDoModelo3d
{
    public static IServiceCollection AddModelo3d(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SolidCraftOptions>().Bind(configuration.GetSection(SolidCraftOptions.Secao));
        services.AddHostedService<VerificacaoSolidCraft>();
        services.AddSingleton<IMedidorDoModelo, MedidorDoModeloSolidCraft>();
        services.AddSingleton<IEditorDoModelo, EditorDoModeloSolidCraft>();
        services.AddSingleton<IMontadorDoModelo, MontadorDoModeloSolidCraft>();
        return services;
    }
}
