namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Função da peça na composição de uma arte. Gravado como texto no banco.
/// </summary>
public enum TipoPeca
{
    Elo = 1,
    Fecho = 2,
    Pedra = 3,
    Pingente = 4,
    Corrente = 5,
    Engaste = 6,
    Outro = 99,
}
