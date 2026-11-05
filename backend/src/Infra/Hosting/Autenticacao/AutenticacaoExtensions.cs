using Castwork.Application.Abstractions;
using Castwork.Application.Acesso.Common;
using Castwork.Application.Common.Security;
using Castwork.Infra.Auth.Jwt;
using Castwork.Infra.Auth.Permissoes;
using Castwork.Infra.Auth.Senhas;
using Castwork.Infra.Auth.Sessao;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Castwork.Infra.Hosting.Autenticacao;

public static class AutenticacaoExtensions
{
    public static IServiceCollection AddAutenticacao(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = ParametrosDeValidacao.Criar(jwt.Value);
            });

        // Negar por padrão: todo endpoint exige usuário autenticado, salvo [AllowAnonymous] explícito.
        var autorizacao = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Uma policy por regra do catálogo, com o código como nome: [Authorize(Policy = "pecas.ler")].
        foreach (var regra in CatalogoDeRegras.Todas)
        {
            autorizacao.AddPolicy(regra.Codigo, policy => policy.RequireAuthenticatedUser().AddRequirements(new RegraRequirement(regra.Id)));
        }

        services.AddScoped<IAuthorizationHandler, RegraAuthorizationHandler>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddScoped<ISessaoService, SessaoService>();
        services.AddScoped<IPermissaoService, PermissaoService>();
        services.AddSingleton<ISenhaHasher, SenhaHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<EmissorDeSessao>();

        return services;
    }
}
