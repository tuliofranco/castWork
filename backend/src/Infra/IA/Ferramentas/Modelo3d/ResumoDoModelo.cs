using System.Globalization;
using System.Text;
using System.Text.Json;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// O modelo em uma linha por corpo, a partir do <c>measure</c> do SolidCraft: o que a IA precisa para conferir uma
/// alteração sem pedir imagem. Ex.: "Aro (fechado, Castwork liga 2): 20 × 20 × 3 mm · volume 261,54 mm³ · faces: 2
/// cilindro, 2 plano". Corpo com o material "Default" do SolidCraft (ou sem nenhum) sai como "SEM MATERIAL", e uma
/// última linha lista esses corpos.
/// </summary>
internal static class ResumoDoModelo
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string? Resumir(string medida)
    {
        try
        {
            using var documento = JsonDocument.Parse(medida);
            if (!documento.RootElement.TryGetProperty("bodies", out var corpos) || corpos.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var linhas = new StringBuilder("Modelo agora:");
            if (corpos.GetArrayLength() == 0)
            {
                return "Modelo agora: nenhum corpo sólido.";
            }

            var semMaterial = new List<string>();
            foreach (var corpo in corpos.EnumerateArray())
            {
                linhas.Append("\n- ").Append(Linha(corpo, semMaterial));
            }

            if (semMaterial.Count > 0)
            {
                linhas.Append("\nSem material (atribua com material.assign): ").Append(string.Join(", ", semMaterial)).Append('.');
            }

            return linhas.ToString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Linha(JsonElement corpo, List<string> semMaterial)
    {
        var partes = new List<string>();
        var nome = corpo.TryGetProperty("name", out var n) ? n.GetString() ?? "Corpo" : "Corpo";
        var fechado = corpo.TryGetProperty("closed", out var c) && c.ValueKind == JsonValueKind.True;
        var material = corpo.TryGetProperty("material", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        if (string.IsNullOrWhiteSpace(material) || material == MateriaisDoModelo.SemMaterial)
        {
            material = "SEM MATERIAL";
            semMaterial.Add(nome);
        }

        if (corpo.TryGetProperty("bbox", out var caixa) && caixa.TryGetProperty("min", out var min) && caixa.TryGetProperty("max", out var max))
        {
            var medidas = Enumerable.Range(0, 3).Select(i => Numero(max[i].GetDouble() - min[i].GetDouble()));
            partes.Add($"{string.Join(" × ", medidas)} mm");
        }

        if (corpo.TryGetProperty("volume_mm3", out var volume) && volume.ValueKind == JsonValueKind.Number)
        {
            partes.Add($"volume {Numero(volume.GetDouble())} mm³");
        }

        if (corpo.TryGetProperty("face_types", out var faces) && faces.ValueKind == JsonValueKind.Object)
        {
            partes.Add("faces: " + string.Join(", ", faces.EnumerateObject().Select(f => $"{f.Value} {f.Name}")));
        }

        return $"{nome} ({(fechado ? "fechado" : "aberto")}, {material}): {string.Join(" · ", partes)}";
    }

    private static string Numero(double valor) => Math.Round(valor, 2).ToString("0.##", PtBr);
}
