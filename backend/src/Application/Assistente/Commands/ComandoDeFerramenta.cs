using Castwork.Application.Projetos.Commands;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Commands sobre a ferramenta de uma versão (<c>/projetos/{projetoId}/versoes/{versaoId}/ferramentas/{ferramenta}</c>).
/// </summary>
public abstract class ComandoDeFerramenta : ComandoDeVersao
{
    public string Ferramenta { get; set; } = string.Empty;
}
