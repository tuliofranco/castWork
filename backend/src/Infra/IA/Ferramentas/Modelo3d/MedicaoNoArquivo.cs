using System.Text.Json;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// As chamadas do SolidCraft que medem o arquivo e a leitura do resultado: o <c>measure</c> (corpos com material, volume,
/// massa e se é fechado) e o <c>inspect.interference</c> (pares de corpos que se sobrepõem, com o volume em comum).
/// </summary>
internal static class MedicaoNoArquivo
{
    /// <summary>
    /// Segundos que a checagem de sobreposição pode levar: uma joia tem poucos corpos, isso sobra.
    /// </summary>
    public const int TempoDaInterferenciaEmSegundos = 20;

    public static (string Nome, string Argumentos) Medida { get; } = ("measure", "{}");

    public static (string Nome, string Argumentos) Interferencia { get; } =
        ("execute", $$$"""{"command":"inspect.interference","params":{"budget_s":{{{TempoDaInterferenciaEmSegundos}}}}}""");

    /// <summary>
    /// Os corpos do <c>measure</c>. Volume e massa só vêm de corpo fechado; material ausente fica nulo. Nulo se não for
    /// o resultado esperado.
    /// </summary>
    public static IReadOnlyList<CorpoMedido>? LerCorpos(string medida)
    {
        try
        {
            using var documento = JsonDocument.Parse(medida);
            if (!documento.RootElement.TryGetProperty("bodies", out var corpos) || corpos.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return [.. corpos.EnumerateArray().Select(c => new CorpoMedido(
                Texto(c, "name") ?? "Corpo",
                Texto(c, "material"),
                Numero(c, "volume_mm3"),
                Numero(c, "mass_g"),
                c.TryGetProperty("closed", out var fechado) && fechado.ValueKind == JsonValueKind.True))];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Os pares que se sobrepõem, do <c>inspect.interference</c>. Nulo se não for o resultado esperado.
    /// </summary>
    public static IReadOnlyList<SobreposicaoMedida>? LerSobreposicoes(string interferencia)
    {
        try
        {
            using var documento = JsonDocument.Parse(interferencia);
            if (!documento.RootElement.TryGetProperty("interferences", out var pares) || pares.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return [.. pares.EnumerateArray()
                .Where(p => Texto(p, "a") is not null && Texto(p, "b") is not null)
                .Select(p => new SobreposicaoMedida(Texto(p, "a")!, Texto(p, "b")!, Math.Round(Numero(p, "volume_mm3") ?? 0m, 3)))];
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Texto(JsonElement elemento, string campo)
        => elemento.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    private static decimal? Numero(JsonElement elemento, string campo)
        => elemento.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.Number && valor.TryGetDouble(out var numero)
            && double.IsFinite(numero) && Math.Abs(numero) < 1e15
            ? (decimal)numero
            : null;
}
