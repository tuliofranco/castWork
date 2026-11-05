using System.Reflection;

using Castwork.Application.Common.Security;

namespace Castwork.Infra.MediatR.Behaviours;

/// <summary>
/// Atributos de segurança de cada tipo de request, lidos por reflexão uma única vez:
/// o campo estático de uma classe genérica é inicializado uma vez por <typeparamref name="TRequest"/>.
/// </summary>
internal static class PermissaoGuidCache<TRequest>
{
    public static readonly Guid? Regra = typeof(TRequest).GetCustomAttribute<PermissaoAttribute>()?.Regra;

    public static readonly bool SomenteAutenticado = typeof(TRequest).IsDefined(typeof(SomenteAutenticadoAttribute));

    public static readonly bool SemPermissao = typeof(TRequest).IsDefined(typeof(SemPermissaoAttribute));
}
