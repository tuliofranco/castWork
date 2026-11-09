using System.Globalization;
using System.Text.Json;

namespace Castwork.Infra.IA.Ferramentas.Desenho2d;

/// <summary>
/// O desenho em uma linha, a partir do <c>inspect_drawing</c> do CADCraft: o que a IA precisa para conferir uma
/// alteração sem pedir imagem. Ex.: "Desenho agora: 1 Circle, 1 Line · extensão 24 × 20 mm · camadas: 0, Cotas".
/// </summary>
internal static class ResumoDoDesenho
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string? Resumir(string inspecao)
    {
        try
        {
            using var documento = JsonDocument.Parse(inspecao);
            var raiz = documento.RootElement;
            if (!raiz.TryGetProperty("counts", out var contagem) || contagem.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var entidades = contagem.EnumerateObject().Select(c => $"{c.Value} {c.Name}").ToList();
            if (entidades.Count == 0)
            {
                return "Desenho agora: vazio.";
            }

            var partes = new List<string> { string.Join(", ", entidades) };
            if (raiz.TryGetProperty("extents", out var extensao) && extensao.ValueKind == JsonValueKind.Array && extensao.GetArrayLength() == 2)
            {
                var (min, max) = (extensao[0], extensao[1]);
                partes.Add($"extensão {Numero(max[0].GetDouble() - min[0].GetDouble())} × {Numero(max[1].GetDouble() - min[1].GetDouble())} mm");
            }

            if (raiz.TryGetProperty("layers", out var camadas) && camadas.ValueKind == JsonValueKind.Array)
            {
                partes.Add("camadas: " + string.Join(", ", camadas.EnumerateArray().Select(c => c.GetProperty("name").GetString())));
            }

            return "Desenho agora: " + string.Join(" · ", partes);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string Numero(double valor) => Math.Round(valor, 2).ToString("0.##", PtBr);
}
