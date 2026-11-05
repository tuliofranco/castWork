using Microsoft.AspNetCore.Authentication;

namespace Castwork.WebAPI.Configuracao;

public static class RejeitarTokenInvalido
{
    /// <summary>
    /// Requisição com <c>Authorization: Bearer</c> que não autenticou (expirado, adulterado) recebe 401 já aqui.
    /// Sem isto, rotas anônimas (como /graphql, que autoriza campo a campo) tratariam o token vencido como
    /// "anônimo" e responderiam 200 com erro de autorização, e o front não saberia que deve renovar o token.
    /// </summary>
    public static IApplicationBuilder UseRejeitarTokenInvalido(this IApplicationBuilder app)
        => app.Use(async (contexto, proximo) =>
        {
            var temBearer = contexto.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            if (temBearer && contexto.User.Identity?.IsAuthenticated != true)
            {
                await contexto.ChallengeAsync();
                return;
            }

            await proximo(contexto);
        });
}
