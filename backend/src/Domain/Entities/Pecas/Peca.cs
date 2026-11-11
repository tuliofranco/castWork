using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Componente reutilizável de uma arte (um elo, um fecho, uma pedra) na biblioteca única do ateliê. Não tem valor: o
/// valor fica na cópia que cada projeto usa (<see cref="Projetos.PecaDoProjeto"/>), que nasce no projeto e chega aqui ao
/// ser publicada. Peça em uso não é excluída, é desativada.
/// </summary>
public sealed partial class Peca : BaseEntity<long>, IAggregateRoot, IAuditavel, IAtivavel
{
    public const int NomeMaxLength = 150;

    /// <summary>
    /// Cabe o nome de qualquer liga, que é copiado para cá.
    /// </summary>
    public const int MaterialMaxLength = Liga.NomeMaxLength;
    public const int DescricaoMaxLength = 2000;

    private readonly List<DesenhoDaPeca> _desenhos = [];

    private Peca()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public TipoPeca Tipo { get; private set; }

    /// <summary>
    /// A liga do ateliê escolhida (peças de metal: todo tipo menos pedra). Nula na pedra, sem liga ou nas peças anteriores
    /// à escolha por liga. Ver <see cref="MaterialDaPeca"/>.
    /// </summary>
    public long? LigaId { get; private set; }

    /// <summary>
    /// Nome da liga copiado quando ela foi escolhida ("Ouro 18k amarelo"), ou o material livre da pedra ("Turmalina").
    /// </summary>
    public string? Material { get; private set; }

    /// <summary>
    /// Teor copiado da liga (750 = 18k). Nulo na pedra.
    /// </summary>
    public TeorMetal? Teor { get; private set; }

    public Peso? Peso { get; private set; }

    public string? Descricao { get; private set; }

    public bool Ativo { get; private set; }

    /// <summary>
    /// O desenho 2D e o modelo 3D da peça, copiados do projeto ao publicar (só para ver e para importar). Não é carregado
    /// junto com a peça: quem precisa pede.
    /// </summary>
    public IReadOnlyCollection<DesenhoDaPeca> Desenhos => _desenhos.AsReadOnly();

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
