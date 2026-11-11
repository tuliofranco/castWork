using Castwork.Application.Abstractions;

namespace Castwork.Application.Common.Security;

public static class PermissaoServiceExtensions
{
    /// <summary>
    /// Se o usuário da requisição tem a regra, com a mesma decisão do <c>AuthorizationBehaviour</c>: sem usuário, não;
    /// admin, sim; senão, o perfil precisa ter a regra. Para dados que só uma parte dos usuários de um command pode ver.
    /// </summary>
    public static async Task<bool> SessaoPossuiRegraAsync(
        this IPermissaoService permissaoService, ISessaoService sessao, string regra, CancellationToken cancellationToken)
    {
        if (!sessao.EstaAutenticado)
        {
            return false;
        }

        return sessao.IsAdmin
            || (sessao.PerfilId is long perfilId && await permissaoService.PerfilPossuiRegraAsync(perfilId, Guid.Parse(regra), cancellationToken));
    }
}
