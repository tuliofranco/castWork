using Castwork.Application.Abstractions;

using Microsoft.AspNetCore.Authorization;

namespace Castwork.Infra.Auth.Permissoes;

/// <summary>
/// Exige uma regra do catálogo. Uma policy por regra, com o código como nome (<c>pecas.ler</c>), permite usar
/// <c>[Authorize(Policy = "pecas.ler")]</c> nos resolvers GraphQL que não passam pelo MediatR.
/// </summary>
public sealed class RegraRequirement(Guid regra) : IAuthorizationRequirement
{
    public Guid Regra { get; } = regra;
}

/// <summary>
/// Mesma decisão do <c>AuthorizationBehaviour</c>: admin passa; senão, o perfil precisa ter a regra.
/// Consulta o banco (com cache), não a claim <c>permissoes</c> do token, que pode estar desatualizada.
/// </summary>
public sealed class RegraAuthorizationHandler(ISessaoService sessao, IPermissaoService permissaoService)
    : AuthorizationHandler<RegraRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RegraRequirement requirement)
    {
        if (sessao.UsuarioId is null)
        {
            return;
        }

        if (sessao.IsAdmin
            || (sessao.PerfilId is long perfilId && await permissaoService.PerfilPossuiRegraAsync(perfilId, requirement.Regra, CancellationToken.None)))
        {
            context.Succeed(requirement);
        }
    }
}
