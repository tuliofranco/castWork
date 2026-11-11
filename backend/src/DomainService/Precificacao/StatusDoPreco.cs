namespace Castwork.DomainService.Precificacao;

public enum StatusDoPreco
{
    Completo = 1,
    Incompleto = 2,
}

public enum TipoDePendencia
{
    /// <summary>
    /// Peça da composição sem valor no projeto.
    /// </summary>
    PecaSemValor = 1,

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

    /// <summary>
    /// Corpos do modelo 3D sem material do ateliê (inclui o "Default" do SolidCraft): o material é obrigatório.
    /// </summary>
    CorposSemMaterial = 6,

    /// <summary>
    /// A versão tem modelo 3D, mas não foi possível medi-lo (SolidCraft indisponível ou arquivo com problema).
    /// </summary>
    ModeloNaoMedido = 7,
}

public enum TipoDeAviso
{
    /// <summary>
    /// Cotação de um metal usado com mais de <see cref="CalculadoraDePreco.DiasParaCotacaoAntiga"/> dias.
    /// </summary>
    CotacaoAntiga = 1,

    /// <summary>
    /// Dois corpos de metal do modelo 3D se sobrepõem: a parte comum é contada duas vezes no peso.
    /// </summary>
    CorposSobrepostos = 2,

    /// <summary>
    /// Corpo aberto no modelo 3D: o volume não é confiável e ele fica fora do peso.
    /// </summary>
    CorpoAberto = 3,
}

/// <summary>
/// O que o material do corpo diz sobre ele no preço.
/// </summary>
public enum UsoDoCorpo
{
    /// <summary>
    /// Material de uma liga do ateliê: entra no peso do metal.
    /// </summary>
    Metal = 1,

    /// <summary>
    /// "Pedra": fora do peso (o custo da pedra vem da composição).
    /// </summary>
    Pedra = 2,

    /// <summary>
    /// "Apoio (não conta)": rascunho, guia ou corpo auxiliar, fora do cálculo.
    /// </summary>
    Apoio = 3,

    /// <summary>
    /// Sem material do ateliê (inclui o "Default" do SolidCraft): deixa o preço incompleto.
    /// </summary>
    SemMaterial = 4,
}

/// <summary>
/// De onde vem o peso do metal da versão.
/// </summary>
public enum OrigemDoPeso
{
    /// <summary>
    /// Digitado na versão (versão sem modelo 3D): estimado manualmente.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Medido no arquivo 3D atual da versão.
    /// </summary>
    Modelo = 2,
}
