using Castwork.Application.Abstractions;
using Castwork.Infra.IA.Assistente;
using Castwork.Infra.IA.Assistente.Conversa;
using Castwork.Infra.IA.Ferramentas.Desenho2d;
using Castwork.Infra.IA.Ferramentas.Modelo3d;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA;

public static class DependencyInjection
{
    /// <summary>
    /// O Assistente (a conversa com a IA) e o lado da IA de cada ferramenta. Sem chave configurada a API sobe
    /// normalmente; só as ferramentas respondem 503 até a chave existir. Sem o CADCraft ou o
    /// SolidCraft, só a ferramenta que depende dele fica indisponível.
    /// </summary>
    public static IServiceCollection AddIa(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.Secao));

        services.AddScoped<IAssistenteFerramenta, AssistenteFerramentaClaude>();

        // Cada ferramenta registra o seu lado da IA (Ferramentas/<Nome>); o Assistente as descobre.
        var ferramentas = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IFerramentaNaIa).IsAssignableFrom(t));
        foreach (var ferramenta in ferramentas)
        {
            services.AddSingleton(typeof(IFerramentaNaIa), ferramenta);
        }

        services.AddDesenho2d(configuration);
        services.AddModelo3d(configuration);
        services.AddSingleton<IPainelDoTurno, PainelDoTurnoEmMemoria>();
        services.AddMemoryCache();
        services.AddSingleton<IPreviaFerramenta, PreviaFerramentaMcp>();
        return services;
    }
}
