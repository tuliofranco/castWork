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

        return services;
    }
}
