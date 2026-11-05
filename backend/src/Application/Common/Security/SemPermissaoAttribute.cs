namespace Castwork.Application.Common.Security;

/// <summary>
/// Marca explicitamente um request público, que roda sem usuário (ex.: login, refresh token).
/// Existe para que "público" seja uma decisão visível no código, e não um esquecimento.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SemPermissaoAttribute : Attribute;
