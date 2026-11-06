using System.Text.Json;

namespace Castwork.Infra.IA.Mcp;

/// <summary>
/// Uma ferramenta anunciada pelo servidor MCP, com o esquema de entrada como veio.
/// </summary>
internal sealed record FerramentaMcp(string Nome, string Descricao, JsonElement Esquema);
