using Castwork.Domain.Common;
using Castwork.Domain.Entities.Clientes;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Peça encomendada, do briefing ao pós-venda. Cada transição é um método com nome de negócio
/// (<see cref="Aprovar"/>, <see cref="Entregar"/>…) que valida a máquina de estados, fecha a etapa atual,
/// abre a próxima e publica um evento específico.
/// </summary>
public sealed partial class Peca : BaseEntity<long>, IAggregateRoot, IAuditavel, IPorStatus<StatusPeca>
{
    public const int TituloMaxLength = 150;
    public const int DescricaoMaxLength = 4000;
    public const int MotivoMaxLength = 500;

    private readonly List<EtapaPeca> _etapas = [];
    private readonly List<ObservacaoPeca> _observacoes = [];

    private Peca()
    {
    }

    public long ClienteId { get; private set; }

    /// <summary>
    /// Navegação só para leitura (projeções e DataLoaders). O vínculo é sempre alterado por <see cref="ClienteId"/>;
    /// a peça nunca modifica o cliente, que é outro agregado.
    /// </summary>
    public Cliente? Cliente { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string? Descricao { get; private set; }

    public StatusPeca Status { get; private set; }

    public DateOnly? PrazoEntrega { get; private set; }

    public DateTimeOffset? EntregueEm { get; private set; }

    public string? MotivoCancelamento { get; private set; }

    public IReadOnlyCollection<EtapaPeca> Etapas => _etapas.AsReadOnly();

    public IReadOnlyCollection<ObservacaoPeca> Observacoes => _observacoes.AsReadOnly();

    public EtapaPeca? EtapaAtual => _etapas.SingleOrDefault(e => e.EmAndamento);

    public bool EstaEncerrada => TransicoesPeca.EstaEncerrada(Status);

    public IReadOnlySet<StatusPeca> ProximosStatus => TransicoesPeca.ProximosDe(Status);

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
