using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Uma liga usada na versão e o peso dela na peça pronta. O peso é <b>estimado</b> (digitado nesta etapa; medido no
/// modelo 3D nas próximas). O peso real é preenchido depois de pesar a peça e não muda o preço: serve de conferência.
/// </summary>
public sealed record MetalDaVersao
{
    /// <summary>
    /// 10 kg: acima disso é erro de digitação.
    /// </summary>
    public const decimal PesoMaximoGramas = 10_000m;

    public MetalDaVersao(long ligaId, Peso pesoEstimado, Peso? pesoReal)
    {
        LigaId = Guard.IdValido(ligaId, nameof(LigaId));
        PesoEstimado = PesoValido(pesoEstimado, nameof(PesoEstimado));
        PesoReal = pesoReal is null ? null : PesoValido(pesoReal, nameof(PesoReal));
    }

    public long LigaId { get; }

    /// <summary>
    /// Peso estimado da peça pronta nesta liga (sem a perda de fundição).
    /// </summary>
    public Peso PesoEstimado { get; }

    /// <summary>
    /// Peso medido na peça pronta. Nulo enquanto não foi pesada.
    /// </summary>
    public Peso? PesoReal { get; }

    internal MetalDaVersao ComPesoReal(Peso? pesoReal) => new(LigaId, PesoEstimado, pesoReal);

    private static Peso PesoValido(Peso peso, string campo)
    {
        ArgumentNullException.ThrowIfNull(peso);
        return peso > Peso.Zero && peso.Gramas <= PesoMaximoGramas
            ? peso
            : throw new ValorInvalidoException(campo, $"deve ser maior que zero e até {PesoMaximoGramas:N0} g");
    }
}
