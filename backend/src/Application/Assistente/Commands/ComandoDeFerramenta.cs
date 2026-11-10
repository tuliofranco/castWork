using Castwork.Application.Projetos.Commands;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Commands sobre a ferramenta de uma versão (<c>/projetos/{projetoId}/versoes/{versaoId}/ferramentas/{ferramenta}</c>), de
/// uma peça do projeto (<c>/projetos/{projetoId}/pecas/{pecaDoProjetoId}/ferramentas/{ferramenta}</c>) ou do projeto
/// (<c>/projetos/{projetoId}/ferramentas/{ferramenta}</c>).
/// </summary>
public abstract class ComandoDeFerramenta : ComandoDeVersao, IEscopoDaFerramenta
{
    public string Ferramenta { get; set; } = string.Empty;

    /// <summary>
    /// A peça do projeto da rota; 0 fora da peça.
    /// </summary>
    public long PecaDoProjetoId { get; set; }
}
