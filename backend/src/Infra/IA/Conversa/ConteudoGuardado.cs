using System.Text.Json;
using System.Text.Json.Serialization;

namespace Castwork.Infra.IA.Conversa;

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
/// Um bloco da mensagem. <see cref="Tipo"/>: texto, imagem (<see cref="Id"/> é a chave), pensamento,
/// pensamentoOculto, ferramenta (pedido da IA) ou resultado (resposta do sistema, com
/// <see cref="Imagens"/> quando a ferramenta devolveu imagens, como o render do desenho).
/// </summary>
internal sealed record BlocoGuardado(
    string Tipo,
    string? Texto = null,
    string? Assinatura = null,
    string? Id = null,
    string? Nome = null,
    Dictionary<string, JsonElement>? Entrada = null,
    bool? Erro = null,
    IReadOnlyList<string>? Imagens = null);
