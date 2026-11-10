using Castwork.Application.Abstractions;
using Castwork.Infra.Documentos.WordCraft;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.Documentos;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WordCraftOptions>().Bind(configuration.GetSection(WordCraftOptions.Secao));
        services.AddScoped<ExecutorWordCraft>();
        services.AddScoped<IGeradorDocumentoBriefing, GeradorBriefingWordCraft>();
        services.AddScoped<IGeradorFichaTecnica, GeradorFichaTecnicaWordCraft>();
        return services;
    }
}
