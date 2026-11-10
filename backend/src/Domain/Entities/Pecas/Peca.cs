using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Componente reutilizável de uma arte (um elo, um fecho, uma pedra). Fica numa biblioteca única:
/// a mesma peça entra em versões de projetos diferentes. Peça em uso não é excluída, é desativada.
/// </summary>
public sealed partial class Peca : BaseEntity<long>, IAggregateRoot, IAuditavel, IAtivavel
{
    public const int NomeMaxLength = 150;
    public const int MaterialMaxLength = 60;
    public const int DescricaoMaxLength = 2000;

    private Peca()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public TipoPeca Tipo { get; private set; }

    /// <summary>
    /// Ex.: "Ouro amarelo", "Prata", "Turmalina".
    /// </summary>
    public string? Material { get; private set; }

    /// <summary>
    /// Só para metais (750 = 18k).
    /// </summary>
    public TeorMetal? Teor { get; private set; }

    public Peso? Peso { get; private set; }

    public string? Descricao { get; private set; }

    /// <summary>
    /// Custo de compra ou produção de uma unidade (não é o preço de venda). Peça sem custo deixa o preço da versão incompleto.
    /// </summary>
    public Dinheiro? Custo { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
