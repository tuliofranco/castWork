using Castwork.Domain.Common;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Uma imagem guardada do desenho da peça da biblioteca (<see cref="DesenhoDaPeca"/>), copiada das imagens do arquivo da
/// peça do projeto ao publicar e levada de volta a outro projeto ao importar. Só é criada pelo <see cref="DesenhoDaPeca"/>.
/// </summary>
public sealed class ImagemDoDesenhoDaPeca : BaseEntity<long>
{
    private ImagemDoDesenhoDaPeca()
    {
    }

    public string Vista { get; private set; } = string.Empty;

    public string Tipo { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    internal static ImagemDoDesenhoDaPeca Criar(string vista, byte[] conteudo) => new()
    {
        Vista = VistaDaImagem.EhValida(vista) ? vista : throw new ValorInvalidoException(nameof(Vista), "inválida"),
        Tipo = conteudo is { Length: > 0 and <= ImagemDoArquivo.TamanhoMaximo }
            ? TipoDaImagemGuardada.Detectar(conteudo) ?? throw new ValorInvalidoException("Imagem", "deve ser PNG ou SVG")
            : throw new ValorInvalidoException("Imagem", $"deve ter até {ImagemDoArquivo.TamanhoMaximo / (1024 * 1024)} MB"),
        Conteudo = conteudo,
    };
}
