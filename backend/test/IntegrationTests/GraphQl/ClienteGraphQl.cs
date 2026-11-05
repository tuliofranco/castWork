using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Castwork.IntegrationTests.GraphQl;

/// <summary>
/// Cliente mínimo para os testes: POST /graphql com query e variáveis, devolve o JSON da resposta.
/// </summary>
internal sealed class ClienteGraphQl(HttpClient http, string? token)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string? Token { get; } = token;

    public static string[] CodigosDeErro(JsonNode resposta)
        => resposta["errors"]?.AsArray().Select(e => e!["extensions"]?["code"]?.GetValue<string>() ?? string.Empty).ToArray() ?? [];

    public static T Ler<T>(JsonNode? no) => no.Deserialize<T>(Json)!;

    public async Task<JsonNode> ExecutarAsync(string query, object? variaveis = null)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri("/graphql", UriKind.Relative))
        {
            Content = JsonContent.Create(new { query, variables = variaveis }),
        };

        if (Token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        using var resposta = await http.SendAsync(requisicao);
        return JsonNode.Parse(await resposta.Content.ReadAsStringAsync())!;
    }
}
