using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Castwork.Application.Ferramentas.Modelo3d;

namespace Castwork.Infra.IA.Ferramentas.Modelo3d;

/// <summary>
/// As chamadas do SolidCraft que montam o modelo da versão a partir dos modelos das peças e a leitura do que ele devolve:
/// <c>component.insert_design</c> (traz os corpos de outro arquivo num componente novo, ligado a ele, com o material de
/// cada corpo) seguido de <c>component.break_link</c> (o componente fica com os corpos e esquece o arquivo, que só existe
/// na pasta temporária), o <c>body.rename</c> que dá aos corpos de cada unidade o nome dela, a faixa em X dos corpos (pelo <c>measure</c>) para enfileirar as unidades, e o
/// <c>material.assign</c> que acerta os materiais depois.
/// </summary>
internal static partial class MontagemNoArquivo
{
    /// <summary>
    /// Espaço entre uma unidade e a próxima na fila (mm).
    /// </summary>
    public const decimal Espaco = 5m;

    public static (string Nome, string Argumentos) Inserir(string arquivo, string nome, decimal deslocamentoX)
        => ("execute", new JsonObject
        {
            ["command"] = "component.insert_design",
            ["params"] = new JsonObject
            {
                ["path"] = arquivo,
                ["name"] = nome,
                ["translate"] = new JsonArray((double)deslocamentoX, 0.0, 0.0),
            },
        }.ToJsonString());

    public static (string Nome, string Argumentos) Desligar(long componente)
        => ("execute", new JsonObject
        {
            ["command"] = "component.break_link",
            ["params"] = new JsonObject { ["component"] = componente },
        }.ToJsonString());

    /// <summary>
    /// O componente criado pelo <c>component.insert_design</c> e os corpos que ele trouxe; nulo se não for o resultado esperado.
    /// </summary>
    public static (long Componente, IReadOnlyList<string> Corpos)? LerInsercao(string resultado)
    {
        try
        {
            using var documento = JsonDocument.Parse(resultado);
            var raiz = documento.RootElement;
            if (!raiz.TryGetProperty("component", out var componente) || !componente.TryGetInt64(out var id))
            {
                return null;
            }

            var corpos = raiz.TryGetProperty("created_bodies", out var criados) && criados.ValueKind == JsonValueKind.Array
                ? criados.EnumerateArray().Where(c => c.ValueKind == JsonValueKind.String).Select(c => c.GetString()!).ToList()
                : [];
            return (id, corpos);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Do menor ao maior X ocupado pelos corpos do <c>measure</c>; nulo sem corpos (ou se não for a medida esperada).
    /// </summary>
    public static (decimal Min, decimal Max)? FaixaEmX(string medida)
    {
        try
        {
            using var documento = JsonDocument.Parse(medida);
            if (!documento.RootElement.TryGetProperty("bodies", out var corpos) || corpos.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            (decimal Min, decimal Max)? faixa = null;
            foreach (var corpo in corpos.EnumerateArray())
            {
                if (X(corpo, "min") is not { } min || X(corpo, "max") is not { } max)
                {
                    continue;
                }

                faixa = faixa is { } f ? (Math.Min(f.Min, min), Math.Max(f.Max, max)) : (min, max);
            }

            return faixa;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// O <c>material.assign</c> (num <c>batch</c>, tudo ou nada) que deixa cada corpo trazido com um material do ateliê:
    /// o material da peça que veio renomeado porque a densidade no arquivo dela era outra (<c>Castwork liga 2 (linked 1)</c>)
    /// volta ao nome do ateliê, e o corpo sem material (o "Default") recebe a liga da peça. Nulo quando não há o que acertar.
    /// <paramref name="ligaDoCorpo"/>: o material da liga da peça de cada corpo trazido (sem liga ativa, fica fora).
    /// </summary>
    public static (string Nome, string Argumentos)? AcertarMateriais(string medida, IReadOnlyDictionary<string, string> ligaDoCorpo, IReadOnlyList<MaterialDoModelo> materiais)
    {
        var doAtelie = materiais.Select(m => m.Nome).ToHashSet(StringComparer.Ordinal);
        var atribuicoes = new List<(string Corpo, string Material)>();
        try
        {
            using var documento = JsonDocument.Parse(medida);
            if (!documento.RootElement.TryGetProperty("bodies", out var corpos) || corpos.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var corpo in corpos.EnumerateArray())
            {
                var nome = Texto(corpo, "name");
                if (nome is null || !ligaDoCorpo.TryGetValue(nome, out var liga))
                {
                    continue;
                }

                var material = Texto(corpo, "material");
                if (material is not null && MaterialRenomeado().Match(material) is { Success: true } renomeado && doAtelie.Contains(renomeado.Groups[1].Value))
                {
                    atribuicoes.Add((nome, renomeado.Groups[1].Value));
                }
                else if ((material is null || material == MateriaisDoModelo.SemMaterial) && liga.Length > 0)
                {
                    atribuicoes.Add((nome, liga));
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return atribuicoes.Count == 0
            ? null
            : ("batch", new JsonObject
            {
                ["commands"] = new JsonArray([.. atribuicoes
                    .GroupBy(a => a.Material, StringComparer.Ordinal)
                    .Select(g => (JsonNode)new JsonObject
                    {
                        ["command"] = "material.assign",
                        ["params"] = new JsonObject
                        {
                            ["bodies"] = new JsonArray([.. g.Select(a => (JsonNode)JsonValue.Create(a.Corpo))]),
                            ["material"] = g.Key,
                        },
                    })]),
            }.ToJsonString());
    }

    /// <summary>
    /// Os corpos trazidos numa unidade com o nome dela ("Elo oval (3)"; vários: "Elo oval (3)", "Elo oval (3) (2)"), num
    /// <c>batch</c> de <c>body.rename</c>; nulo quando já têm esses nomes. Devolve também o nome novo de cada corpo.
    /// </summary>
    public static ((string Nome, string Argumentos)? Chamada, IReadOnlyList<string> Nomes) RenomearCorpos(IReadOnlyList<string> corpos, string unidade)
    {
        ArgumentNullException.ThrowIfNull(corpos);
        var renomeacoes = NomesDosCorpos.Renomeacoes(corpos, unidade);
        var nomes = NomesDosCorpos.Com(unidade, corpos.Count);
        return renomeacoes.Count == 0
            ? (null, nomes)
            : (("batch", new JsonObject
            {
                ["commands"] = new JsonArray([.. renomeacoes.Select(r => (JsonNode)new JsonObject
                {
                    ["command"] = "body.rename",
                    ["params"] = new JsonObject { ["body"] = r.De, ["name"] = r.Para },
                })]),
            }.ToJsonString()), nomes);
    }

    /// <summary>
    /// Material com o mesmo nome de um do arquivo mas outra definição: o SolidCraft o renomeia ao inserir.
    /// </summary>
    [GeneratedRegex(@"^(.+) \(linked \d+\)$", RegexOptions.CultureInvariant)]
    private static partial Regex MaterialRenomeado();

    private static decimal? X(JsonElement corpo, string ponta)
        => corpo.TryGetProperty("bbox", out var caixa) && caixa.ValueKind == JsonValueKind.Object
            && caixa.TryGetProperty(ponta, out var ponto) && ponto.ValueKind == JsonValueKind.Array && ponto.GetArrayLength() > 0
            && ponto[0].ValueKind == JsonValueKind.Number && ponto[0].TryGetDecimal(out var x)
                ? x
                : null;

    private static string? Texto(JsonElement elemento, string campo)
        => elemento.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}
