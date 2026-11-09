using System.Text;
using System.Text.Json;

namespace Castwork.Infra.IA.Assistente.Conversa;

/// <summary>
/// O resultado de uma ferramenta do MCP do jeito mais curto para a IA: JSON sem espaços, números com até
/// 4 casas (o <c>-1.04e-15</c> do kernel vira 0), sem nulos, sem listas e objetos vazios e sem os campos que a
/// ferramenta marca como internos. Texto que não é JSON passa como veio.
/// </summary>
internal static class ResultadoCompacto
{
    private const int Casas = 4;

    public static string Compactar(string texto, IReadOnlySet<string>? ocultos = null)
    {
        var inicio = texto.AsSpan().TrimStart();
        if (inicio.IsEmpty || (inicio[0] != '{' && inicio[0] != '['))
        {
            return texto;
        }

        try
        {
            using var documento = JsonDocument.Parse(texto);
            using var saida = new MemoryStream();
            using (var escritor = new Utf8JsonWriter(saida, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                Escrever(escritor, documento.RootElement, ocultos ?? new HashSet<string>());
            }

            return Encoding.UTF8.GetString(saida.ToArray());
        }
        catch (JsonException)
        {
            return texto;
        }
    }

    private static void Escrever(Utf8JsonWriter escritor, JsonElement valor, IReadOnlySet<string> ocultos)
    {
        switch (valor.ValueKind)
        {
            case JsonValueKind.Object:
                escritor.WriteStartObject();
                foreach (var propriedade in valor.EnumerateObject())
                {
                    if (!ocultos.Contains(propriedade.Name) && !Vazio(propriedade.Value))
                    {
                        escritor.WritePropertyName(propriedade.Name);
                        Escrever(escritor, propriedade.Value, ocultos);
                    }
                }

                escritor.WriteEndObject();
                break;
            case JsonValueKind.Array:
                escritor.WriteStartArray();
                foreach (var item in valor.EnumerateArray())
                {
                    Escrever(escritor, item, ocultos);
                }

                escritor.WriteEndArray();
                break;
            case JsonValueKind.Number when !valor.TryGetInt64(out _) && valor.TryGetDouble(out var numero):
                var arredondado = Math.Round(numero, Casas);
                if (arredondado == Math.Truncate(arredondado) && Math.Abs(arredondado) < long.MaxValue)
                {
                    escritor.WriteNumberValue((long)arredondado);
                }
                else
                {
                    escritor.WriteNumberValue(arredondado);
                }

                break;
            default:
                valor.WriteTo(escritor);
                break;
        }
    }

    private static bool Vazio(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.Null => true,
        JsonValueKind.Array => valor.GetArrayLength() == 0,
        JsonValueKind.Object => !valor.EnumerateObject().Any(),
        _ => false,
    };
}
