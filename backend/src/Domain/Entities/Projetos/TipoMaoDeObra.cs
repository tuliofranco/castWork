namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Como a mão de obra da versão é cobrada. Gravado como texto no banco.
/// </summary>
public enum TipoMaoDeObra
{
    /// <summary>
    /// Horas de bancada × valor da hora do projeto.
    /// </summary>
    PorHoras = 1,

    /// <summary>
    /// Um valor combinado para a peça inteira.
    /// </summary>
    ValorFixo = 2,
}
