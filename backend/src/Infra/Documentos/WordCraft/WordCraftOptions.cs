namespace Castwork.Infra.Documentos.WordCraft;

/// <summary>
/// Seção <c>WordCraft</c> da configuração: onde está o <c>wordcraft-cli</c> neste servidor.
/// </summary>
public sealed class WordCraftOptions
{
    public const string Secao = "WordCraft";

    public string? Caminho { get; set; }

    public int TempoLimiteEmSegundos { get; set; } = 60;
}
