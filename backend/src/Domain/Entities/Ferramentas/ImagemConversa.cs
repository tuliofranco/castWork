using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// Imagem de uma conversa de ferramenta: anexada pelo joalheiro ou gerada pela IA (o <c>render</c> do
/// desenho). Agregado à parte para a conversa não carregar os arquivos.
/// </summary>
public sealed class ImagemConversa : BaseEntity<long>, IAggregateRoot
{
    public const int TamanhoMaximo = 5 * 1024 * 1024;

    private ImagemConversa()
    {
    }

    public long ConversaId { get; private set; }

    public Guid Chave { get; private set; }

    public string Tipo { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>
    /// <paramref name="chave"/> informada quando a imagem já foi referenciada antes de existir (a IA gera o
    /// render no meio do turno); senão nasce aqui.
    /// </summary>
    public static ImagemConversa Criar(long conversaId, byte[] conteudo, DateTimeOffset em, Guid? chave = null)
    {
        if (conteudo is not { Length: > 0 } || conteudo.Length > TamanhoMaximo)
        {
            throw new ValorInvalidoException("Imagem", $"deve ter até {TamanhoMaximo / (1024 * 1024)} MB");
        }

        return new ImagemConversa
        {
            ConversaId = Guard.IdValido(conversaId, nameof(ConversaId)),
            Chave = chave ?? Guid.CreateVersion7(),
            Tipo = TipoDeImagem.Detectar(conteudo) ?? throw new ValorInvalidoException("Imagem", "deve ser PNG, JPEG ou WebP"),
            Conteudo = conteudo,
            CriadaEm = em,
        };
    }
}
