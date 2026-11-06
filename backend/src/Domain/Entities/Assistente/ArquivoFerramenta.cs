using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Assistente;

/// <summary>
/// Uma versão do arquivo de trabalho da ferramenta (o DXF do desenho 2D). O histórico só cresce: o mais
/// recente é o atual, e desfazer ou restaurar grava de novo uma versão anterior. Nada é apagado: até
/// voltar ao desenho em branco vira uma versão (vazia).
/// </summary>
public sealed class ArquivoFerramenta : BaseEntity<long>, IAggregateRoot
{
    public const int TamanhoMaximo = 20 * 1024 * 1024;
    public const int NomeMaxLength = 120;

    private ArquivoFerramenta()
    {
    }

    public long ConversaId { get; private set; }

    public byte[] Conteudo { get; private set; } = [];

    public OrigemArquivo Origem { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>
    /// Nome dado pelo joalheiro; sem nome, a tela mostra o da ferramenta com o número (ex.: "Desenho 2D #5").
    /// </summary>
    public string? Nome { get; private set; }

    /// <summary>
    /// Arquivado: some da pasta e do histórico e deixa de poder ser o atual. Só arquivado pode ser apagado.
    /// </summary>
    public DateTimeOffset? ArquivadoEm { get; private set; }

    public bool Arquivado => ArquivadoEm is not null;

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

    public void Renomear(string? nome)
        => Nome = string.IsNullOrWhiteSpace(nome) ? null : Guard.Obrigatorio(nome.Trim(), nameof(Nome), NomeMaxLength);

    public void Arquivar(DateTimeOffset em)
    {
        if (Arquivado)
        {
            throw new RegraDeNegocioException("O arquivo já está arquivado.");
        }

        ArquivadoEm = em;
    }

    public void Desarquivar()
    {
        if (!Arquivado)
        {
            throw new RegraDeNegocioException("O arquivo não está arquivado.");
        }

        ArquivadoEm = null;
    }

    /// <summary>
    /// Apagar é definitivo e só vale para arquivado: arquivar antes é a confirmação de que não faz falta.
    /// </summary>
    public void GarantirQuePodeApagar()
    {
        if (!Arquivado)
        {
            throw new RegraDeNegocioException("Arquive o arquivo antes de apagar.");
        }
    }
}
