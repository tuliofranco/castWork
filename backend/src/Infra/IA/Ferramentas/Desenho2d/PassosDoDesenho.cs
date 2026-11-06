using System.Text.Json;

namespace Castwork.Infra.IA.Ferramentas.Desenho2d;

/// <summary>
/// Traduz cada ferramenta que a IA chama no CADCraft num passo legível para o joalheiro acompanhar
/// ("Desenhando arcos", "Cotando"). Devolve nulo para o que não precisa aparecer.
/// </summary>
internal static class PassosDoDesenho
{
    private static readonly (string[] Comandos, string Passo)[] PorComando =
    [
        (["line", "l", "pline", "pl", "polyline", "xline", "ray"], "Desenhando linhas"),
        (["circle", "c"], "Desenhando círculos"),
        (["arc", "a"], "Desenhando arcos"),
        (["ellipse", "el"], "Desenhando elipses"),
        (["rectang", "rec", "rectangle", "polygon", "pol"], "Desenhando contornos"),
        (["spline", "spl"], "Desenhando curvas"),
        (["dim", "dimlinear", "dli", "dimaligned", "dal", "dimradius", "dra", "dimdiameter", "ddi", "dimangular", "dan", "leader", "mleader", "qdim"], "Cotando"),
        (["text", "dtext", "mtext", "t", "mt"], "Escrevendo textos"),
        (["hatch", "h", "bhatch"], "Hachurando"),
        (["layer", "la", "-layer"], "Organizando camadas"),
        (["offset", "o"], "Fazendo paralelas"),
        (["trim", "tr", "extend", "ex", "fillet", "f", "chamfer", "cha", "break", "br", "join", "j"], "Acertando cantos e encontros"),
        (["move", "m", "copy", "co", "cp", "rotate", "ro", "mirror", "mi", "scale", "sc", "array", "ar", "stretch", "s"], "Ajustando posições"),
        (["erase", "e", "delete"], "Apagando partes"),
        (["zoom", "z", "pan", "p", "regen"], "Enquadrando o desenho"),
    ];

    public static string? Descrever(string ferramenta, IReadOnlyDictionary<string, JsonElement>? entrada) => ferramenta switch
    {
        "render" => "Olhando o desenho",
        "inspect_drawing" or "query_entities" or "find_text" => "Conferindo o desenho",
        "list_commands" => "Consultando os comandos do CADCraft",
        "command_line" or "script" => PorTexto(Texto(entrada, "text")),
        "execute" => PorTexto(Texto(entrada, "command")),
        "cancel" => null,
        _ => "Trabalhando no desenho",
    };

    private static string PorTexto(string? texto)
    {
        var comando = texto?.TrimStart().Split([' ', '\n', '{'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
        if (comando is null)
        {
            return "Trabalhando no desenho";
        }

        // execute usa ids com ponto (layer.new, dim.linear): vale a parte antes do ponto.
        var raiz = comando.Split('.')[0];
        return PorComando.FirstOrDefault(p => p.Comandos.Contains(comando) || p.Comandos.Contains(raiz)).Passo ?? "Desenhando";
    }

    private static string? Texto(IReadOnlyDictionary<string, JsonElement>? entrada, string campo)
        => entrada is not null && entrada.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}
