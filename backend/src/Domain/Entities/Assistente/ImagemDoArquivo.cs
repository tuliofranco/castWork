using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Uma imagem em alta qualidade de uma versão do arquivo da ferramenta (<see cref="ArquivoFerramenta"/>), numa vista
/// (<see cref="VistaDaImagem"/>): gerada em segundo plano depois de salvar, ou copiada da biblioteca ao importar. Agregado
/// à parte para o arquivo não carregar as imagens. Uma por vista e tipo (o 2D guarda o SVG e o PNG).
/// </summary>
public sealed class ImagemDoArquivo : BaseEntity<long>, IAggregateRoot
{
    public const int TamanhoMaximo = 20 * 1024 * 1024;

    private ImagemDoArquivo()
    {
    }

    public long ArquivoId { get; private set; }

    public string Vista { get; private set; } = string.Empty;

    public string Tipo { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    public DateTimeOffset CriadaEm { get; private set; }

    public static ImagemDoArquivo Criar(long arquivoId, string vista, byte[] conteudo, DateTimeOffset em) => new()
    {
        ArquivoId = Guard.IdValido(arquivoId, nameof(ArquivoId)),
        Vista = VistaDaImagem.EhValida(vista) ? vista : throw new ValorInvalidoException(nameof(Vista), "inválida"),
        Tipo = conteudo is { Length: > 0 and <= TamanhoMaximo }
            ? TipoDaImagemGuardada.Detectar(conteudo) ?? throw new ValorInvalidoException("Imagem", "deve ser PNG ou SVG")
            : throw new ValorInvalidoException("Imagem", $"deve ter até {TamanhoMaximo / (1024 * 1024)} MB"),
        Conteudo = conteudo,
        CriadaEm = em,
    };
}
