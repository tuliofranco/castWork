using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Um PDF gerado do briefing. Agregado à parte para a conversa não carregar os arquivos;
/// cada geração cria um novo documento e os anteriores ficam guardados.
/// </summary>
public sealed partial class DocumentoBriefing : BaseEntity<long>, IAggregateRoot
{
    private DocumentoBriefing()
    {
    }

    public long BriefingId { get; private set; }

    public string NomeArquivo { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    public DateTimeOffset GeradoEm { get; private set; }
}
