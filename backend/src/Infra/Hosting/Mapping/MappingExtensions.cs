using Castwork.Application.Common.Mapping;

using MapsterMapper;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.Hosting.Mapping;

public static class MappingExtensions
{
    /// <summary>
    /// Configuração do Mapster é singleton e compilada na inicialização: mapa inválido derruba a subida da API.
    /// </summary>
    public static IServiceCollection AddMapeamento(this IServiceCollection services)
    {
        var config = MappingConfiguration.Criar();
        config.Compile();

        services.AddSingleton(config);
        services.AddSingleton<IMapper>(new Mapper(config));
        return services;
    }
}
