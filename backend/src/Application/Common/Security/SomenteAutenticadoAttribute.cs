namespace Castwork.Application.Common.Security;

/// <summary>
/// Basta estar logado, sem regra específica (ex.: "meus dados", trocar a própria senha).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SomenteAutenticadoAttribute : Attribute;
