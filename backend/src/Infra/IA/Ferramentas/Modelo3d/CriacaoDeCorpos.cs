using System.Text.Json;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// No modelo 3D da versão, a IA só monta e posiciona as peças: os comandos do SolidCraft que criam corpos (sólidos,
/// primitivas, superfícies, chapas, malhas e inserção de outro arquivo) são recusados antes de chegar ao MCP, no
/// <c>execute</c> e em qualquer comando de um <c>batch</c> (que é tudo ou nada). Na peça, o 3D continua livre.
/// </summary>
internal static class CriacaoDeCorpos
{
    public const string Motivo =
        "Recusado: no modelo da versão você só monta e posiciona as peças, sem criar corpos. Se faltar uma peça, "
        + "oriente o joalheiro a modelá-la no Modelo 3D da própria peça e marcar o arquivo final.";

    /// <summary>
    /// Os comandos que criam corpos (ou trazem corpos de fora do Castwork).
    /// </summary>
    public static readonly IReadOnlySet<string> Comandos = new HashSet<string>(StringComparer.Ordinal)
    {
        "solid.extrude", "solid.revolve", "solid.sweep", "solid.loft", "solid.pipe", "solid.coil", "solid.rib", "solid.web",
        "solid.box", "solid.cylinder", "solid.cone", "solid.sphere", "solid.torus", "solid.emboss", "solid.boundary_fill",
        "solid.bounding_solid", "solid.thread", "solid.split_body",
        "surface.extrude", "surface.revolve", "surface.sweep", "surface.loft", "surface.patch", "surface.boundary",
        "surface.ruled", "surface.blend", "surface.offset", "surface.thicken", "surface.import",
        "sheet.convert", "sheet.flange", "sheet.path_flange", "mesh.convert",
        "component.insert_design", "component.paste_from_design",
    };

    /// <summary>
    /// O motivo da recusa quando a chamada cria corpos; nulo deixa passar.
    /// </summary>
    public static string? Recusar(string ferramenta, IReadOnlyDictionary<string, JsonElement> entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        return ferramenta switch
        {
            "execute" => Cria(entrada.GetValueOrDefault("command")) ? Motivo : null,
            "batch" => entrada.TryGetValue("commands", out var comandos) && comandos.ValueKind == JsonValueKind.Array
                && comandos.EnumerateArray().Any(c => c.ValueKind == JsonValueKind.Object && c.TryGetProperty("command", out var comando) && Cria(comando))
                    ? Motivo
                    : null,
            _ => null,
        };
    }

    private static bool Cria(JsonElement comando) => comando.ValueKind == JsonValueKind.String && Comandos.Contains(comando.GetString()!);
}
