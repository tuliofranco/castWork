using Castwork.Application.Abstractions;
using Castwork.Infra.IA.Briefing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA;

public static class DependencyInjection
{
    /// <summary>
    /// Sem chave configurada a API sobe normalmente; só o briefing responde 503 até a chave existir.
    /// </summary>
    public static IServiceCollection AddIa(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.Secao));
        services.AddScoped<IAssistenteBriefing, AssistenteBriefingClaude>();
        return services;
    }
}
