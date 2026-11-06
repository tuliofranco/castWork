using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ferramentas;

/// <summary>
/// Uma versão do arquivo de trabalho da ferramenta (o DXF do desenho 2D). O histórico só cresce: o mais
/// recente é o atual, e desfazer ou restaurar grava de novo uma versão anterior. Nada é apagado: até
/// voltar ao desenho em branco vira uma versão (vazia).
/// </summary>
public sealed class ArquivoFerramenta : BaseEntity<long>, IAggregateRoot
{
    public const int TamanhoMaximo = 20 * 1024 * 1024;

    private ArquivoFerramenta()
    {
    }

    public long ConversaId { get; private set; }

    public byte[] Conteudo { get; private set; } = [];

    public OrigemArquivo Origem { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Versão sem desenho: o Desfazer da ação que criou o primeiro arquivo volta a ela.
    /// </summary>
    public bool EmBranco => Conteudo.Length == 0;

    public static ArquivoFerramenta Criar(long conversaId, byte[] conteudo, OrigemArquivo origem, DateTimeOffset em) => new()
    {
        ConversaId = Guard.IdValido(conversaId, nameof(ConversaId)),
        Conteudo = conteudo is { Length: > 0 and <= TamanhoMaximo }
            ? conteudo
            : throw new ValorInvalidoException("Arquivo", $"deve ter até {TamanhoMaximo / (1024 * 1024)} MB"),
        Origem = Enum.IsDefined(origem) ? origem : throw new ValorInvalidoException(nameof(Origem), "inválida"),
        CriadoEm = em,
    };

    /// <summary>
    /// O desenho em branco, gravado no histórico em vez de apagar as versões que vieram antes.
    /// </summary>
    public static ArquivoFerramenta EmBrancoPara(long conversaId, DateTimeOffset em) => new()
    {
        ConversaId = Guard.IdValido(conversaId, nameof(ConversaId)),
        Origem = OrigemArquivo.Desfazer,
        CriadoEm = em,
    };

    /// <summary>
    /// Cópia desta versão como a atual (desfazer ou restaurar do histórico).
    /// </summary>
    public ArquivoFerramenta Copiar(OrigemArquivo origem, DateTimeOffset em) => new()
    {
        ConversaId = ConversaId,
        Conteudo = Conteudo,
        Origem = Enum.IsDefined(origem) ? origem : throw new ValorInvalidoException(nameof(Origem), "inválida"),
        CriadoEm = em,
    };
}
