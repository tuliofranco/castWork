namespace Castwork.Infra.IA.Assistente;

/// <summary>
/// O lado da IA de uma ferramenta: instruções, servidor MCP e arquivo de trabalho (<see cref="DefinicaoDeFerramenta"/>).
/// Cada ferramenta implementa o seu em <c>Ferramentas/&lt;Nome&gt;</c>; o Assistente as descobre pela injeção de
/// dependência e não conhece nenhuma pelo nome.
/// </summary>
internal interface IFerramentaNaIa
{
    DefinicaoDeFerramenta Definicao { get; }
}
