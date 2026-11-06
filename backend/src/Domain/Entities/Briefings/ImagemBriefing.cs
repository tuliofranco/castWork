using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Uma foto ou croqui que o joalheiro mandou na conversa do briefing. Agregado à parte, como os PDFs,
/// para a conversa não carregar os arquivos. A <see cref="Chave"/> nasce com a imagem: a mensagem e a IA
/// a referenciam antes mesmo de a imagem ir para o banco.
/// </summary>
public sealed partial class ImagemBriefing : BaseEntity<long>, IAggregateRoot
{
    public const int TamanhoMaximo = 5 * 1024 * 1024;

    private ImagemBriefing()
    {
    }

    public long BriefingId { get; private set; }

    public Guid Chave { get; private set; }

    /// <summary>
    /// Tipo MIME, detectado pelo conteúdo (image/png, image/jpeg ou image/webp).
    /// </summary>
    public string Tipo { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    public DateTimeOffset EnviadaEm { get; private set; }
}
