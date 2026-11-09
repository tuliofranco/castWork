using System.Text.Json;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Traduz cada ferramenta que a IA chama no SolidCraft num passo legível para o joalheiro acompanhar
/// ("Esboçando", "Arredondando arestas"). Devolve nulo para o que não precisa aparecer.
/// </summary>
internal static class PassosDoModelo
{
    // Comandos do SolidCraft têm ids com ponto (solid.extrude, sketch.circle.center): vale o mais específico.
    private static readonly (string[] Comandos, string Passo)[] Passos =
    [
        (["solid.extrude"], "Extrudando"),
        (["solid.revolve"], "Revolucionando o perfil"),
        (["solid.sweep", "solid.loft"], "Criando superfícies de varredura"),
        (["solid.fillet"], "Arredondando arestas"),
        (["solid.chamfer"], "Chanfrando arestas"),
        (["solid.shell"], "Ocando o modelo"),
        (["solid.hole", "solid.thread"], "Furando"),
        (["solid.pattern", "solid.mirror"], "Repetindo formas"),
        (["solid.combine", "solid.split"], "Combinando corpos"),
        (["solid"], "Modelando sólidos"),
        (["sketch"], "Esboçando"),
        (["parameter", "param"], "Ajustando parâmetros"),
        (["timeline"], "Editando a linha do tempo"),
        (["material"], "Definindo o material"),
        (["view", "camera"], "Enquadrando o modelo"),
        (["edit"], "Ajustando o modelo"),
    ];

    public static string? Descrever(string ferramenta, IReadOnlyDictionary<string, JsonElement>? entrada) => ferramenta switch
    {
        "screenshot" or "compare_picture" => "Olhando o modelo",
        "inspect_design" or "measure" or "body_topology" or "check_parameters" => "Conferindo o modelo",
        "list_commands" or "describe_command" or "skill_reference" => "Consultando os comandos do SolidCraft",
        "set_parameter" => "Ajustando parâmetros",
        "undo" => "Desfazendo um passo",
        "redo" => "Refazendo um passo",
        "execute" => PorComando(Texto(entrada, "command")),
        "batch" => PorBatch(entrada),
        "pending" or "running" => null,
        _ => "Trabalhando no modelo",
    };

    /// <summary>
    /// Um batch conta pelo comando que dá forma ao modelo (o primeiro sólido), não pelo esboço que o prepara.
    /// </summary>
    private static string PorBatch(IReadOnlyDictionary<string, JsonElement>? entrada)
    {
        if (entrada is null || !entrada.TryGetValue("commands", out var comandos) || comandos.ValueKind != JsonValueKind.Array)
        {
            return "Modelando";
        }

        var ids = comandos.EnumerateArray()
            .Select(c => c.ValueKind == JsonValueKind.Object && c.TryGetProperty("command", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null)
            .OfType<string>()
            .ToList();
        return PorComando(ids.FirstOrDefault(id => id.StartsWith("solid.", StringComparison.OrdinalIgnoreCase)) ?? ids.FirstOrDefault());
    }

    private static string PorComando(string? comando)
    {
        if (string.IsNullOrWhiteSpace(comando))
        {
            return "Modelando";
        }

        var id = comando.Trim().ToLowerInvariant();
        return Passos.FirstOrDefault(p => p.Comandos.Any(c => id == c || id.StartsWith(c + ".", StringComparison.Ordinal))).Passo ?? "Modelando";
    }

    private static string? Texto(IReadOnlyDictionary<string, JsonElement>? entrada, string campo)
        => entrada is not null && entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}
