namespace Castwork.DomainService.Precificacao;

public enum StatusDoPreco
{
    Completo = 1,
    Incompleto = 2,
}

public enum TipoDePendencia
{
    /// <summary>
    /// Peça da composição sem custo na biblioteca.
    /// </summary>
    PecaSemCusto = 1,

    /// <summary>
    /// Metal de uma liga da versão sem cotação no projeto.
    /// </summary>
    MetalSemCotacao = 2,

    /// <summary>
    /// A versão não tem nenhum metal com peso.
    /// </summary>
    SemMetalComPeso = 3,

    /// <summary>
    /// Liga desativada no ateliê e ainda usada na versão.
    /// </summary>
    LigaDesativada = 4,

    /// <summary>
    /// Mão de obra por horas, mas o valor da hora do projeto está "a definir" (zero).
    /// </summary>
    ValorHoraADefinir = 5,
}

public enum TipoDeAviso
{
    /// <summary>
    /// Cotação de um metal usado com mais de <see cref="CalculadoraDePreco.DiasParaCotacaoAntiga"/> dias.
    /// </summary>
    CotacaoAntiga = 1,
}
