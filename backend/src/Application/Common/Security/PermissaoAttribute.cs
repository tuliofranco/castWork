namespace Castwork.Application.Common.Security;

/// <summary>
/// Exige que o usuário logado tenha a regra (ou seja admin). Use as constantes de <see cref="Regras"/>,
/// nunca um GUID solto: é de lá que o módulo Acesso cadastra as regras no banco.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PermissaoAttribute(string regra) : Attribute
{
    public Guid Regra { get; } = Guid.Parse(regra);
}
