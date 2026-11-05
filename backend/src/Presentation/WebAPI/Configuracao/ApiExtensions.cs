using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Castwork.WebAPI.Configuracao;

public static class ApiExtensions
{
    public const string PoliticaCors = "front";

    public static IServiceCollection AddApiHttp(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment ambiente)
    {
        services.AddControllers(options => options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer())))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = contexto =>
            contexto.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<TratamentoDeExcecoes>();

        // Lista explícita de origens; nunca AllowAnyOrigin. Em dev o Angular usa proxy (mesma origem),
        // então isto só vale para quem chamar a API direto.
        var origens = configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy(PoliticaCors, politica => politica
            .WithOrigins(origens)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type")));

        services.AddLimitesDeRequisicao(configuration);

        if (ambiente.IsDevelopment())
        {
            services.AddOpenApi();
        }

        return services;
    }
}
