namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Seção <c>SolidCraft</c> da configuração: onde está o <c>solidcraft-cli</c> neste servidor. É ele que,
/// pelo MCP (<c>solidcraft-cli mcp</c>), vai deixar a IA modelar nos arquivos 3D das versões.
/// </summary>
public sealed class SolidCraftOptions
{
    public const string Secao = "SolidCraft";

    public string? Caminho { get; set; }

    public int TempoLimiteEmSegundos { get; set; } = 10;
}
