namespace Castwork.WebAPI.Configuracao;

public static class HeadersDeSeguranca
{
    /// <summary>
    /// Headers de defesa em toda resposta. A API só devolve JSON, então a CSP é a mais restrita possível;
    /// as páginas do Scalar e do Nitro ficam de fora.
    /// </summary>
    public static IApplicationBuilder UseHeadersDeSeguranca(this IApplicationBuilder app)
        => app.Use(async (contexto, proximo) =>
        {
            var headers = contexto.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            // Páginas de ferramenta (Scalar e Nitro, só em desenvolvimento) precisam carregar script e estilo.
            var ehFerramenta = contexto.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
                || contexto.Request.Path.StartsWithSegments("/graphql", StringComparison.OrdinalIgnoreCase);
            if (!ehFerramenta)
            {
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            }

            await proximo(contexto);
        });
}
