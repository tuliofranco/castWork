using System.Text.Json;
using System.Text.Json.Nodes;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// Os materiais do Castwork no arquivo 3D: a partir do <c>material.list</c> do arquivo aberto, os comandos que faltam
/// para ele ter os materiais pedidos (<see cref="MaterialDoModelo"/>). Material que falta: <c>material.create</c>; material
/// do arquivo com outra densidade (a liga mudou no ateliê): <c>material.edit</c>. O que já está certo fica como está, e
/// nada é apagado (corpos podem usar o material de uma liga desativada).
/// </summary>
internal static class MateriaisNoArquivo
{
    /// <summary>
    /// A densidade da liga tem 3 casas: diferença menor que meia casa é a mesma.
    /// </summary>
    private const double Tolerancia = 0.0005;

    public static (string Nome, string Argumentos) Leitura { get; } = ("execute", """{"command":"material.list"}""");

    /// <summary>
    /// A chamada (<c>batch</c>, que aplica tudo ou nada) que deixa o arquivo com os materiais do preparo; nula quando já
    /// estão todos certos ou a lista do arquivo não pôde ser lida.
    /// </summary>
    public static (string Nome, string Argumentos)? Garantir(string lista, string preparo)
    {
        var comandos = Comandos(lista, MateriaisDoModelo.LerPreparo(preparo));
        return comandos.Count == 0
            ? null
            : ("batch", new JsonObject { ["commands"] = new JsonArray([.. comandos]) }.ToJsonString());
    }

    public static IReadOnlyList<JsonObject> Comandos(string lista, IReadOnlyList<MaterialDoModelo> materiais)
    {
        if (Existentes(lista) is not { } existentes)
        {
            return [];
        }

        var comandos = new List<JsonObject>();
        foreach (var material in materiais.DistinctBy(m => m.Nome))
        {
            if (!existentes.TryGetValue(material.Nome, out var existente))
            {
                comandos.Add(Comando("material.create", Criacao(material)));
            }
            else if (existente.DoArquivo && Math.Abs(existente.Densidade - (double)material.Densidade) > Tolerancia)
            {
                comandos.Add(Comando("material.edit", new JsonObject
                {
                    ["material"] = material.Nome,
                    ["density_g_cm3"] = (double)material.Densidade,
                }));
            }
        }

        return comandos;
    }

    private static JsonObject Criacao(MaterialDoModelo material)
    {
        var parametros = new JsonObject
        {
            ["name"] = material.Nome,
            ["density_g_cm3"] = (double)material.Densidade,
            ["color"] = material.Cor,
        };
        if (material.Opacidade < 1)
        {
            parametros["opacity"] = (double)material.Opacidade;
        }

        // Metal: brilho de peça polida (o PBR só vale para material opaco).
        if (material.Metalico && material.Opacidade >= 1)
        {
            parametros["pbr"] = new JsonObject { ["metallic"] = 1.0, ["roughness"] = 0.10, ["dielectric_ior"] = 1.5 };
        }

        return parametros;
    }

    private static JsonObject Comando(string comando, JsonObject parametros) => new() { ["command"] = comando, ["params"] = parametros };

    /// <summary>
    /// Nome → densidade e se o material é do arquivo (os do SolidCraft não se editam). Nulo se não for a lista esperada.
    /// </summary>
    private static Dictionary<string, (double Densidade, bool DoArquivo)>? Existentes(string lista)
    {
        try
        {
            using var documento = JsonDocument.Parse(lista);
            if (!documento.RootElement.TryGetProperty("materials", out var materiais) || materiais.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var existentes = new Dictionary<string, (double, bool)>(StringComparer.Ordinal);
            foreach (var material in materiais.EnumerateArray())
            {
                if (material.TryGetProperty("name", out var nome) && nome.ValueKind == JsonValueKind.String)
                {
                    var densidade = material.TryGetProperty("density_g_cm3", out var d) && d.ValueKind == JsonValueKind.Number ? d.GetDouble() : double.NaN;
                    var doArquivo = material.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.True;
                    existentes[nome.GetString()!] = (densidade, doArquivo);
                }
            }

            return existentes;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
