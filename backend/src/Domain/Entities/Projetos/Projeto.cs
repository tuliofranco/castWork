using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Projeto de uma arte (um colar, uma pulseira). Evolui em versões; cada versão tem a sua composição, feita com as
/// peças do projeto. Versões, itens e peças do projeto pertencem ao agregado: a numeração sequencial, a regra "publicada
/// não muda" e "só entram peças deste projeto" são garantidas aqui.
/// </summary>
public sealed partial class Projeto : BaseEntity<long>, IAggregateRoot, IAuditavel
{
    public const int NomeMaxLength = 150;
    public const int DescricaoMaxLength = 2000;

    private readonly List<VersaoProjeto> _versoes = [];
    private readonly List<CotacaoDoProjeto> _cotacoes = [];
    private readonly List<PecaDoProjeto> _pecas = [];

    private Projeto()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public string? Descricao { get; private set; }

    /// <summary>
    /// Cliente para quem a arte é feita. Opcional: há projetos autorais, sem cliente.
    /// </summary>
    public long? ClienteId { get; private set; }

    public IReadOnlyCollection<VersaoProjeto> Versoes => _versoes.AsReadOnly();

    /// <summary>
    /// Peças do projeto: criadas aqui (rascunho) ou importadas da biblioteca. A composição das versões só usa estas.
    /// </summary>
    public IReadOnlyCollection<PecaDoProjeto> Pecas => _pecas.AsReadOnly();

    /// <summary>
    /// Preços do projeto: cotação de cada metal (só os que o ateliê já cotou, ou que o projeto informou).
    /// </summary>
    public IReadOnlyCollection<CotacaoDoProjeto> Cotacoes => _cotacoes.AsReadOnly();

    /// <summary>
    /// Preços do projeto: perda, valor da hora, markup, taxas e arredondamento, copiados do ateliê.
    /// </summary>
    public ParametrosDePreco ParametrosDePreco { get; private set; } = null!;

    /// <summary>
    /// Quando os preços foram copiados do ateliê (na criação ou ao atualizar para os valores atuais).
    /// </summary>
    public DateTimeOffset PrecosHerdadosEm { get; private set; }

    /// <summary>
    /// Quando os preços foram editados no projeto, divergindo do que foi herdado. Nulo enquanto estão como herdados.
    /// </summary>
    public DateTimeOffset? PrecosAlteradosEm { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
