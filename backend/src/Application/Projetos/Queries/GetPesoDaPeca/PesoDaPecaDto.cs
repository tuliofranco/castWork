namespace Castwork.Application.Projetos.Queries.GetPesoDaPeca;

/// <summary>
/// De onde vem o peso da peça do projeto.
/// </summary>
public enum OrigemDoPesoDaPeca
{
    /// <summary>Sem modelo 3D: o peso digitado nos dados da peça.</summary>
    Digitado = 1,

    /// <summary>Medido no modelo 3D da peça, na liga dela.</summary>
    Modelo = 2,

    /// <summary>Tem modelo 3D, mas a peça não tem liga (pedra ou material livre): vale o digitado.</summary>
    ModeloSemLiga = 3,

    /// <summary>Tem modelo 3D, mas não foi possível medir: sem peso até medir.</summary>
    ModeloNaoMedido = 4,
}

/// <summary>
/// O peso que vale para a peça (g; nulo quando não há) e de onde vem; <see cref="PesoDigitado"/> é o informado nos dados
/// da peça, que fica guardado (e a tela diz que não é usado) quando vale o 3D.
/// </summary>
public sealed record PesoDaPecaDto(long PecaDoProjetoId, decimal? Gramas, OrigemDoPesoDaPeca Origem, decimal? PesoDigitado);
