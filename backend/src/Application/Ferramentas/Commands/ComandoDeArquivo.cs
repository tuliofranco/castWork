namespace Castwork.Application.Ferramentas.Commands;

/// <summary>
/// Commands sobre um arquivo da ferramenta (<c>.../ferramentas/{ferramenta}/arquivo/historico/{arquivoId}</c>).
/// </summary>
public abstract class ComandoDeArquivo : ComandoDeFerramenta
{
    public long ArquivoId { get; set; }
}
