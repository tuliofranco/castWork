namespace Castwork.Application.Assistente;

/// <summary>
/// De quem é a conversa da ferramenta, pela rota: do projeto (<see cref="VersaoId"/> e <see cref="PecaDoProjetoId"/> 0), de
/// uma versão (<c>/versoes/{versaoId}</c>) ou de uma peça do projeto (<c>/pecas/{pecaDoProjetoId}</c>).
/// </summary>
public interface IEscopoDaFerramenta
{
    long ProjetoId { get; }

    /// <summary>
    /// A versão da rota; 0 fora da versão.
    /// </summary>
    long VersaoId { get; }

    /// <summary>
    /// A peça do projeto da rota; 0 fora da peça.
    /// </summary>
    long PecaDoProjetoId { get; }
}

/// <summary>
/// O escopo montado por quem não vem de uma rota (ex.: a conferência do modelo 3D da versão).
/// </summary>
public sealed record EscopoDaFerramenta(long ProjetoId, long VersaoId = 0, long PecaDoProjetoId = 0) : IEscopoDaFerramenta
{
    public static EscopoDaFerramenta DaVersao(long projetoId, long versaoId) => new(projetoId, versaoId);

    public static EscopoDaFerramenta DaPeca(long projetoId, long pecaDoProjetoId) => new(projetoId, PecaDoProjetoId: pecaDoProjetoId);
}
