using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;

namespace Castwork.Application.Assistente;

/// <summary>
/// O que as leituras das imagens guardadas têm em comum (do arquivo da ferramenta e do desenho da peça da biblioteca): o
/// arquivo atual (ou o final) do escopo, o formato pedido e a ordem de mostrar.
/// </summary>
internal static class ImagensGuardadas
{
    /// <summary>
    /// Sem formato, a de melhor qualidade primeiro: o SVG (vetorial) e depois o PNG.
    /// </summary>
    private static readonly List<string> Preferencia = [TipoDaImagemGuardada.Svg, TipoDaImagemGuardada.Png];

    /// <summary>
    /// Os tipos aceitos para o formato pedido, na ordem de preferência; nulo se o formato é desconhecido.
    /// </summary>
    public static List<string>? Tipos(string? formato) => formato?.ToLowerInvariant() switch
    {
        null or "" => Preferencia,
        "png" => [TipoDaImagemGuardada.Png],
        "svg" => [TipoDaImagemGuardada.Svg],
        _ => null,
    };

    /// <summary>
    /// Pela vista (<see cref="VistaDaImagem.Todas"/>) e, na mesma vista, pela preferência do tipo.
    /// </summary>
    public static IReadOnlyList<T> Ordenar<T>(IEnumerable<T> imagens, Func<T, string> vista, Func<T, string> tipo)
        => [.. imagens.OrderBy(i => VistaDaImagem.Ordem(vista(i))).ThenBy(i => Preferencia.IndexOf(tipo(i)))];

    /// <summary>
    /// O Id do arquivo atual (ou, com <paramref name="final"/>, do final da peça) da ferramenta do escopo, sem o desenho em
    /// branco (nulo se não há desenho).
    /// </summary>
    public static async Task<long?> ArquivoAsync(IUnitOfWork unitOfWork, IEscopoDaFerramenta escopo, string ferramenta, bool final, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, escopo, cancellationToken);
        if (await ConversaDoEscopo.IdAsync(unitOfWork, escopo, ferramenta, cancellationToken) is not { } conversaId)
        {
            return null;
        }

        if (final)
        {
            return await unitOfWork.GetRepository<ConversaFerramenta>().FindBy(c => c.Id == conversaId).Select(c => c.ArquivoFinalId).SingleAsync(cancellationToken);
        }

        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversaId && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Id, Vazio = a.Conteudo.Length == 0 })
            .FirstOrDefaultAsync(cancellationToken) is { Vazio: false } atual ? atual.Id : null;
    }
}
