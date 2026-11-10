using System.Text.Json.Nodes;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// As chamadas do SolidCraft que a conferência de peso e materiais faz no arquivo: <c>material.assign</c> (um comando por
/// material, num <c>batch</c>: tudo ou nada) e <c>solid.combine</c> (<c>join</c>: os outros corpos se juntam ao alvo, que
/// fica com o nome e o material dele).
/// </summary>
internal static class EdicaoNoArquivo
{
    public static (string Nome, string Argumentos) Chamada(AlteracaoDoModelo alteracao) => alteracao switch
    {
        DefinicaoDeMateriais definicao => ("batch", new JsonObject
        {
            ["commands"] = new JsonArray([.. definicao.Corpos
                .GroupBy(c => c.Material, StringComparer.Ordinal)
                .Select(g => (JsonNode)Comando("material.assign", new JsonObject
                {
                    ["bodies"] = new JsonArray([.. g.Select(c => (JsonNode)JsonValue.Create(c.Corpo))]),
                    ["material"] = g.Key,
                }))]),
        }.ToJsonString()),
        UniaoDeCorpos uniao => ("execute", Comando("solid.combine", new JsonObject
        {
            ["target"] = uniao.Alvo,
            ["tools"] = new JsonArray([.. uniao.Outros.Select(c => (JsonNode)JsonValue.Create(c))]),
            ["operation"] = "join",
        }).ToJsonString()),
        _ => throw new ArgumentOutOfRangeException(nameof(alteracao), alteracao, "Alteração desconhecida."),
    };

    /// <summary>
    /// Como a alteração aparece no log e na mensagem de recusa.
    /// </summary>
    public static string Descricao(AlteracaoDoModelo alteracao) => alteracao is UniaoDeCorpos ? "unir os corpos" : "definir o material dos corpos";

    private static JsonObject Comando(string comando, JsonObject parametros) => new() { ["command"] = comando, ["params"] = parametros };
}
