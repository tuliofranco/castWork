using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

namespace Castwork.WebAPI.Configuracao;

public static class LimitesDeRequisicao
{
    public const string Autenticacao = "autenticacao";

    /// <summary>
    /// Login e refresh: 10 tentativas por minuto por IP (<c>LimitesDeRequisicao:AutenticacaoPorMinuto</c>). Complementa o bloqueio por conta (5 falhas):
    /// aquele protege uma conta; este segura quem testa a mesma senha em muitas contas.
    /// Atrás de proxy, depende do ForwardedHeaders configurado para o IP ser o do cliente (Etapa 12).
    /// </summary>
    public static IServiceCollection AddLimitesDeRequisicao(this IServiceCollection services, IConfiguration configuration)
    {
        var porMinuto = configuration.GetValue("LimitesDeRequisicao:AutenticacaoPorMinuto", 10);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Autenticacao, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            options.OnRejected = (contexto, _) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    contexto.HttpContext.Response.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };
        });
    }
}
