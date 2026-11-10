using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Liga usada pelo ateliê ("Ouro 18k amarelo"): metal base, teor e densidade. O teor dá a fração de metal puro
/// (preço) e a densidade converte o volume do modelo 3D em peso. Liga em uso não é excluída, é desativada.
/// </summary>
public sealed partial class Liga : BaseEntity<long>, IAggregateRoot, IAuditavel, IAtivavel
{
    public const int NomeMaxLength = 60;

    /// <summary>
    /// Acima do metal mais denso que existe (ósmio, 22,6 g/cm³): valor maior é erro de digitação.
    /// </summary>
    public const decimal DensidadeMaxima = 25m;

    public const int DensidadeCasasDecimais = 3;

    private Liga()
    {
    }

    public string Nome { get; private set; } = string.Empty;

    public Metal Metal { get; private set; }

    public TeorMetal Teor { get; private set; } = TeorMetal.Ouro18k;

    /// <summary>
    /// g/cm³.
    /// </summary>
    public decimal Densidade { get; private set; }

    public bool Ativo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public long? CriadoPor { get; private set; }

    public DateTimeOffset? AlteradoEm { get; private set; }

    public long? AlteradoPor { get; private set; }
}
