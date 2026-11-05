namespace Castwork.WebAPI.Configuracao;

public static class HeadersDeSeguranca
{
    /// <summary>
    /// Headers de defesa em toda resposta. A API só devolve JSON, então a CSP é a mais restrita possível;
    /// a página do Scalar (só em desenvolvimento) fica de fora porque precisa carregar script e estilo.
    /// </summary>
    public static IApplicationBuilder UseHeadersDeSeguranca(this IApplicationBuilder app)
        => app.Use(async (contexto, proximo) =>
        {
            var headers = contexto.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            if (!contexto.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase))
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            await proximo(contexto);
        });
}
