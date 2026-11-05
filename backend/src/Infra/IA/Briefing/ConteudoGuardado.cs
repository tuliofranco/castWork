using System.Text.Json;
using System.Text.Json.Serialization;

namespace Castwork.Infra.IA.Briefing;

/// <summary>
/// Formato próprio com que a conversa é guardada no banco (coluna <c>conteudo</c>). Independe dos tipos do
/// SDK: trocar de versão do SDK não invalida conversas antigas.
/// </summary>
internal sealed record ConteudoGuardado(IReadOnlyList<BlocoGuardado> Blocos)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static ConteudoGuardado Ler(string json) => JsonSerializer.Deserialize<ConteudoGuardado>(json, Json)!;

    public string Gravar() => JsonSerializer.Serialize(this, Json);
}

/// <summary>
/// Um bloco da mensagem. <see cref="Tipo"/>: texto, pensamento, pensamentoOculto, ferramenta (pedido da IA)
/// ou resultado (resposta do sistema).
/// </summary>
internal sealed record BlocoGuardado(
    string Tipo,
    string? Texto = null,
    string? Assinatura = null,
    string? Id = null,
    string? Nome = null,
    Dictionary<string, JsonElement>? Entrada = null,
    bool? Erro = null);
