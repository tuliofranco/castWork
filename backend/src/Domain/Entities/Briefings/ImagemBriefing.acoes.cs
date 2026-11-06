using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Briefings;

/// <summary>
/// Ações da <see cref="ImagemBriefing"/>.
/// </summary>
public sealed partial class ImagemBriefing
{
    public static ImagemBriefing Criar(long briefingId, byte[] conteudo, DateTimeOffset enviadaEm)
    {
        if (conteudo is not { Length: > 0 } || conteudo.Length > TamanhoMaximo)
        {
            throw new ValorInvalidoException("Imagem", $"deve ter até {TamanhoMaximo / (1024 * 1024)} MB");
        }

        return new ImagemBriefing
        {
            BriefingId = Guard.IdValido(briefingId, nameof(BriefingId)),
            Chave = Guid.CreateVersion7(),
            Tipo = TipoDeImagem.Detectar(conteudo) ?? throw new ValorInvalidoException("Imagem", "deve ser PNG, JPEG ou WebP"),
            Conteudo = conteudo,
            EnviadaEm = enviadaEm,
        };
    }
}
