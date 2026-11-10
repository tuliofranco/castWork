using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// O desenho de uma peça da biblioteca numa ferramenta (o DXF do desenho 2D ou o JSON do modelo 3D), copiado da peça do
/// projeto ao publicar. Só para ver e para levar a outro projeto ao importar: na biblioteca ninguém edita o desenho. Um
/// por ferramenta. Só é criado pela <see cref="Peca"/>.
/// </summary>
public sealed class DesenhoDaPeca : BaseEntity<long>
{
    /// <summary>
    /// O mesmo limite do arquivo de trabalho das ferramentas.
    /// </summary>
    public const int TamanhoMaximo = 20 * 1024 * 1024;

    /// <summary>
    /// O mesmo tamanho do código da ferramenta na conversa.
    /// </summary>
    public const int FerramentaMaxLength = 40;

    private readonly List<ImagemDoDesenhoDaPeca> _imagens = [];

    private DesenhoDaPeca()
    {
    }

    /// <summary>
    /// Código da ferramenta que fez o desenho (<c>desenho-2d</c>, <c>modelo-3d</c>).
    /// </summary>
    public string Ferramenta { get; private set; } = string.Empty;

    public byte[] Conteudo { get; private set; } = [];

    /// <summary>
    /// Quando o desenho foi copiado do projeto para a biblioteca (ao publicar).
    /// </summary>
    public DateTimeOffset CopiadoEm { get; private set; }

    /// <summary>
    /// As imagens guardadas do arquivo copiado (uma por vista e tipo); vazio se ainda não tinham sido geradas ao publicar.
    /// </summary>
    public IReadOnlyCollection<ImagemDoDesenhoDaPeca> Imagens => _imagens.AsReadOnly();

    internal static DesenhoDaPeca Criar(string ferramenta, byte[] conteudo, IEnumerable<(string Vista, byte[] Conteudo)> imagens, DateTimeOffset em)
    {
        var desenho = new DesenhoDaPeca
        {
            Ferramenta = Guard.Obrigatorio(ferramenta, nameof(Ferramenta), FerramentaMaxLength),
            Conteudo = conteudo is { Length: > 0 and <= TamanhoMaximo }
                ? conteudo
                : throw new ValorInvalidoException("Desenho", $"deve ter até {TamanhoMaximo / (1024 * 1024)} MB"),
            CopiadoEm = em,
        };
        foreach (var imagem in imagens.Select(i => ImagemDoDesenhoDaPeca.Criar(i.Vista, i.Conteudo)))
        {
            desenho._imagens.RemoveAll(i => i.Vista == imagem.Vista && i.Tipo == imagem.Tipo);
            desenho._imagens.Add(imagem);
        }

        return desenho;
    }
}
