using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;

using MediatR;

namespace Castwork.Infra.MediatR.Behaviours;

/// <summary>
/// 2º do pipeline: antes da validação, para não revelar a quem não tem acesso se um Id existe
/// (a validação consulta o banco). Regras, na ordem:
/// <list type="number">
/// <item><c>[SemPermissao]</c> ou nenhum atributo → passa (os testes de arquitetura impedem request sem atributo);</item>
/// <item>sem usuário → <see cref="UnauthorizedAccessException"/> (401);</item>
/// <item><c>[SomenteAutenticado]</c> ou admin → passa;</item>
/// <item>perfil sem a regra de <c>[Permissao]</c> → <see cref="ForbiddenAccessException"/> (403).</item>
/// </list>
/// </summary>
public sealed class AuthorizationBehaviour<TRequest, TResponse>(ISessaoService sessao, IPermissaoService permissaoService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var regra = PermissaoGuidCache<TRequest>.Regra;
        var exigeLogin = regra is not null || PermissaoGuidCache<TRequest>.SomenteAutenticado;

        if (PermissaoGuidCache<TRequest>.SemPermissao || !exigeLogin)
        {
            return await next(cancellationToken);
        }

        if (sessao.UsuarioId is null)
        {
            throw new UnauthorizedAccessException("Usuário não autenticado.");
        }

        if (regra is null || sessao.IsAdmin)
        {
            return await next(cancellationToken);
        }

        var possuiRegra = sessao.PerfilId is long perfilId
            && await permissaoService.PerfilPossuiRegraAsync(perfilId, regra.Value, cancellationToken);

        return possuiRegra ? await next(cancellationToken) : throw new ForbiddenAccessException();
    }
}
