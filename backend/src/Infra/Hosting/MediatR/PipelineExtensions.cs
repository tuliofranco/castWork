using Castwork.Infra.FluentValidation.Behaviours;
using Castwork.Infra.MediatR.Behaviours;

using Microsoft.Extensions.DependencyInjection;

namespace Castwork.Infra.Hosting.MediatR;

public static class PipelineExtensions
{
    /// <summary>
    /// Registra os handlers e monta o pipeline. A ordem de <c>AddOpenBehavior</c> é a ordem de execução
    /// (o primeiro envolve todos os outros). Fica no Hosting porque junta behaviours de mais de uma Infra.
    /// </summary>
    public static IServiceCollection AddPipelineMediatR(this IServiceCollection services)
        => services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyReference).Assembly);

            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });
}
