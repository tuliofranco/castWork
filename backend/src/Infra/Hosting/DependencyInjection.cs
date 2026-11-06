using Castwork.Application.Assistente;
using Castwork.Infra.Documentos;
using Castwork.Infra.FluentValidation;
using Castwork.Infra.Hosting.Autenticacao;
using Castwork.Infra.Hosting.Mapping;
using Castwork.Infra.Hosting.MediatR;
using Castwork.Infra.Hosting.Persistencia;
using Castwork.Infra.IA;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.Hosting;

/// <summary>
/// Único ponto que conhece todas as implementações de infraestrutura. A Presentation chama só isto.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCastworkInfra(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddAutenticacao(configuration);

        services.AddPersistencia(configuration);
        services.AddPipelineMediatR();
        services.AddMapeamento();
        services.AddValidacao();

        services.AddIa(configuration);
        services.AddDocumentos(configuration);
        services.AddFerramentasDoAssistente();

        return services;
    }

    /// <summary>
    /// Cada ferramenta se registra no Assistente pela sua implementação de <see cref="IFerramentaComIa"/>, na
    /// Application (<c>Ferramentas/&lt;Nome&gt;</c>): ferramenta nova entra sem mexer aqui nem no Assistente.
    /// </summary>
    private static IServiceCollection AddFerramentasDoAssistente(this IServiceCollection services)
    {
        var ferramentas = typeof(Application.AssemblyReference).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IFerramentaComIa).IsAssignableFrom(t));
        foreach (var ferramenta in ferramentas)
        {
            services.AddScoped(typeof(IFerramentaComIa), ferramenta);
        }

        services.AddScoped<CatalogoDeFerramentas>();
        return services;
    }
}
