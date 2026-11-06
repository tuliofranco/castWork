using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas.Briefings;

/// <summary>
/// Ações do <see cref="DocumentoBriefing"/>.
/// </summary>
public sealed partial class DocumentoBriefing
{
    public static DocumentoBriefing Criar(long briefingId, string nomeArquivo, byte[] conteudo, DateTimeOffset geradoEm)
        => new()
        {
            BriefingId = Guard.IdValido(briefingId, nameof(BriefingId)),
            NomeArquivo = Guard.Obrigatorio(nomeArquivo, nameof(NomeArquivo), 200),
            Conteudo = conteudo is { Length: > 0 } ? conteudo : throw new ValorInvalidoException(nameof(Conteudo), "está vazio"),
            GeradoEm = geradoEm,
        };
}
