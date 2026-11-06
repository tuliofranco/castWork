using System.Text.Json;

namespace Castwork.Application.Abstractions;

/// <summary>
/// Ferramentas que a IA usa dentro do próprio Castwork, sem servidor MCP (as do briefing gravam os campos e
/// geram o PDF). A Application define e executa; a Infra só as oferece à IA junto com as do MCP.
/// </summary>
public interface IFerramentasLocais
{
    IReadOnlyList<FerramentaLocal> Definicoes { get; }

    Task<ResultadoLocal> ExecutarAsync(string nome, IReadOnlyDictionary<string, JsonElement> entrada, CancellationToken cancellationToken);
}

/// <summary>
/// Uma ferramenta local: o esquema de entrada é JSON Schema (em modo estrito). <see cref="Passo"/> é como ela
/// aparece no andamento do turno ("Atualizando o briefing").
/// </summary>
public sealed record FerramentaLocal(string Nome, string Descricao, string EsquemaJson, string Passo);

public sealed record ResultadoLocal(string Texto, bool Erro = false);
