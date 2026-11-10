using System.Globalization;
using System.Text;
using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// Um material que o arquivo 3D da versão precisa ter: uma liga ativa do ateliê (com a densidade dela), a "Pedra" ou o
/// "Apoio (não conta)". <see cref="Cor"/> em <c>#rrggbb</c>; <see cref="Metalico"/> dá o brilho de metal no SolidCraft.
/// <see cref="Uso"/> diz à IA quando usar. <see cref="LigaId"/> e <see cref="Liga"/>: só nos de liga.
/// </summary>
public sealed record MaterialDoModelo(
    string Nome,
    decimal Densidade,
    string Cor,
    decimal Opacidade,
    bool Metalico,
    string Uso,
    long? LigaId = null,
    string? Liga = null);

/// <summary>
/// Os materiais do Castwork dentro do arquivo 3D. A classificação de cada corpo (liga, pedra ou apoio) vive no
/// arquivo, com o histórico e o Desfazer dele; o banco só diz quais materiais o arquivo deve ter. O nome do material de
/// uma liga é estável (<c>Castwork liga 2</c>): continua o mesmo se o nome da liga mudar.
/// </summary>
public static class MateriaisDoModelo
{
    public const string PrefixoDaLiga = "Castwork liga ";

    public const string Pedra = "Pedra";

    public const string Apoio = "Apoio (não conta)";

    /// <summary>
    /// O material que o SolidCraft dá a todo corpo (1,29 g/cm³, plástico): para o Castwork, corpo sem material.
    /// </summary>
    public const string SemMaterial = "Default";

    /// <summary>
    /// Só para o SolidCraft aceitar o material: a pedra não entra no peso do metal (o custo dela vem da composição).
    /// </summary>
    public const decimal DensidadeDaPedra = 3.5m;

    public const decimal DensidadeDoApoio = 1m;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string NomeDaLiga(long ligaId) => PrefixoDaLiga + ligaId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A liga de um material do arquivo pelo nome estável; nulo se não é material de liga do Castwork.
    /// </summary>
    public static long? LigaDoMaterial(string? material)
        => material is not null
           && material.StartsWith(PrefixoDaLiga, StringComparison.Ordinal)
           && long.TryParse(material.AsSpan(PrefixoDaLiga.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    /// <summary>
    /// Um material por liga, na ordem recebida, e depois "Pedra" e "Apoio (não conta)".
    /// </summary>
    public static IReadOnlyList<MaterialDoModelo> Montar(IEnumerable<(long Id, string Nome, Metal Metal, decimal Densidade)> ligas)
        =>
        [
            .. ligas.Select(l => new MaterialDoModelo(
                NomeDaLiga(l.Id),
                l.Densidade,
                CorDaLiga(l.Metal, l.Nome),
                1m,
                Metalico: true,
                $"metal: corpo de {l.Nome}",
                l.Id,
                l.Nome)),
            new MaterialDoModelo(Pedra, DensidadeDaPedra, "#cfe6f2", 0.85m, Metalico: false, "pedra: não entra no peso do metal"),
            new MaterialDoModelo(Apoio, DensidadeDoApoio, "#9aa3ad", 0.4m, Metalico: false, "rascunho, guia ou corpo auxiliar: fica fora do cálculo"),
        ];

    /// <summary>
    /// Os materiais das ligas ativas do ateliê (pelo id, estável), mais a pedra e o apoio.
    /// </summary>
    public static async Task<IReadOnlyList<MaterialDoModelo>> CarregarAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var ligas = await unitOfWork.GetRepository<Liga>()
            .FindBy(l => l.Ativo)
            .OrderBy(l => l.Id)
            .Select(l => new { l.Id, l.Nome, l.Metal, l.Densidade })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return Montar(ligas.Select(l => (l.Id, l.Nome, l.Metal, l.Densidade)));
    }

    /// <summary>
    /// A lista como vai no preparo do arquivo (JSON), para a Infra garantir os materiais ao abrir o arquivo.
    /// </summary>
    public static string ParaPreparo(IReadOnlyList<MaterialDoModelo> materiais) => JsonSerializer.Serialize(materiais, Json);

    public static IReadOnlyList<MaterialDoModelo> LerPreparo(string preparo)
        => JsonSerializer.Deserialize<List<MaterialDoModelo>>(preparo, Json) ?? [];

    /// <summary>
    /// Aparência coerente com a liga: prata, ouro branco, ouro rosé ou ouro amarelo (o padrão do ouro).
    /// </summary>
    private static string CorDaLiga(Metal metal, string nome)
    {
        // Cores base PBR medidas (sRGB), as de physicallybased.info: metal reflete a cor, então ela é clara.
        if (metal == Metal.Prata)
        {
            return "#faf8f2";
        }

        var texto = SemAcento(nome);
        return texto.Contains("branco", StringComparison.Ordinal) ? "#dcdcde"
            : texto.Contains("rose", StringComparison.Ordinal) || texto.Contains("rosa", StringComparison.Ordinal) || texto.Contains("vermelho", StringComparison.Ordinal) ? "#f5c6b0"
            : "#ffe29b";
    }

    private static string SemAcento(string texto)
    {
        var decomposto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string([.. decomposto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
    }
}
