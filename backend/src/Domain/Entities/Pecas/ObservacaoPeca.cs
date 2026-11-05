using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Pecas;

public sealed class ObservacaoPeca : BaseEntity<long>
{
    public const int TextoMaxLength = 2000;

    internal ObservacaoPeca(string texto, StatusPeca statusDaPeca, DateTimeOffset criadaEm)
    {
        Texto = Guard.Obrigatorio(texto, nameof(Texto), TextoMaxLength);
        StatusDaPeca = statusDaPeca;
        CriadaEm = criadaEm;
    }

    private ObservacaoPeca()
    {
    }

    public string Texto { get; private set; } = string.Empty;

    /// <summary>
    /// Status em que a peça estava quando a observação foi feita, para a linha do tempo.
    /// </summary>
    public StatusPeca StatusDaPeca { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }
}
