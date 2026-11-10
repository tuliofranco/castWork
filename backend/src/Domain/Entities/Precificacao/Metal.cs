namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Metal com cotação própria. A cotação é sempre do metal <b>puro</b> (ouro 24k, prata fina); a pureza da liga
/// entra no cálculo pelo <see cref="ValueObjects.TeorMetal"/>. Gravado como texto no banco.
/// </summary>
public enum Metal
{
    Ouro = 1,
    Prata = 2,
}
