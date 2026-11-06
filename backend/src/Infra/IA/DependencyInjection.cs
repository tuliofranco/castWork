using Castwork.Application.Abstractions;
using Castwork.Infra.IA.Briefing;
using Castwork.Infra.IA.CadCraft;
using Castwork.Infra.IA.Ferramentas;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.IA;

public static class DependencyInjection
{
    /// <summary>
    /// Sem chave configurada a API sobe normalmente; só o briefing e as ferramentas respondem 503 até a
    /// chave existir. O mesmo vale para o CADCraft: sem ele, só o desenho 2D fica indisponível.
    /// </summary>
    public static IServiceCollection AddIa(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AnthropicOptions>().Bind(configuration.GetSection(AnthropicOptions.Secao));
        services.AddScoped<IAssistenteBriefing, AssistenteBriefingClaude>();

        services.AddOptions<CadCraftOptions>().Bind(configuration.GetSection(CadCraftOptions.Secao));
        services.AddHostedService<VerificacaoCadCraft>();
        services.AddScoped<IAssistenteFerramenta, AssistenteFerramentaClaude>();
        services.AddMemoryCache();
        services.AddSingleton<IPreviaFerramenta, PreviaFerramentaMcp>();
        return services;
    }
}
