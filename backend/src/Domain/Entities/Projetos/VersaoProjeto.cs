using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Uma versão da arte (v1, v2...) com a sua composição e os dados do orçamento (metais, mão de obra, serviços e
/// markup). Só é alterada pelo <see cref="Projeto"/>. O preço não é gravado: é calculado na leitura.
/// </summary>
public sealed partial class VersaoProjeto : BaseEntity<long>
{
    public const int NomeMaxLength = 100;
    public const int ObservacoesMaxLength = 2000;
    public const int MetaisMaximo = 10;
    public const int ServicosMaximo = 20;

    private readonly List<ItemComposicao> _itens = [];

    internal VersaoProjeto(int numero, string nome, string? observacoes)
    {
        Numero = numero;
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Observacoes = Guard.Opcional(observacoes, nameof(Observacoes), ObservacoesMaxLength);
        Status = StatusVersao.EmDesenvolvimento;
    }

    private VersaoProjeto()
    {
    }

    public int Numero { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string? Observacoes { get; private set; }

    public StatusVersao Status { get; private set; }

    public DateTimeOffset? PublicadaEm { get; private set; }

    public IReadOnlyCollection<ItemComposicao> Itens => _itens.AsReadOnly();

    /// <summary>
    /// Ligas da peça com o peso estimado de cada uma (uma versão pode ter ouro amarelo e branco).
    /// </summary>
    public IReadOnlyList<MetalDaVersao> Metais { get; private set; } = [];

    /// <summary>
    /// Nula enquanto não foi informada (entra como zero no custo).
    /// </summary>
    public MaoDeObra? MaoDeObra { get; private set; }

    public IReadOnlyList<ServicoExterno> Servicos { get; private set; } = [];

    /// <summary>
    /// Markup só desta versão. Nulo: vale o do projeto.
    /// </summary>
    public decimal? Markup { get; private set; }
}
