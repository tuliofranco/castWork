namespace Castwork.Infra.IA.Ferramentas.Desenho2d;

/// <summary>
/// Seção <c>CadCraft</c> da configuração: onde está o <c>cadcraft-cli</c> neste servidor. É ele que,
/// pelo MCP (<c>cadcraft-cli mcp</c>), vai deixar a IA desenhar nos arquivos DXF das versões.
/// </summary>
public sealed class CadCraftOptions
{
    public const string Secao = "CadCraft";

    public string? Caminho { get; set; }

    public int TempoLimiteEmSegundos { get; set; } = 10;
}
