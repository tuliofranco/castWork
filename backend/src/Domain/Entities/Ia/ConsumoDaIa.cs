using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Ia;

/// <summary>
/// Tokens que um turno de conversa com a IA consumiu, somando todas as chamadas do turno. Serve para
/// acompanhar o custo por ferramenta e por conversa (o preço por token muda; os tokens não).
/// </summary>
public sealed class ConsumoDaIa : BaseEntity<long>, IAggregateRoot
{
    public const int OrigemMaxLength = 40;
    public const int ModeloMaxLength = 60;

    private ConsumoDaIa()
    {
    }

    /// <summary>
    /// De onde veio o turno: o código da ferramenta (<c>desenho-2d</c>) ou <c>briefing</c>.
    /// </summary>
    public string Origem { get; private set; } = string.Empty;

    /// <summary>
    /// A conversa (da ferramenta ou o briefing) a que o turno pertence.
    /// </summary>
    public long ConversaId { get; private set; }

    public string Modelo { get; private set; } = string.Empty;

    /// <summary>
    /// Quantas chamadas à API o turno fez (uma por ida e volta com as ferramentas).
    /// </summary>
    public int Chamadas { get; private set; }

    public int Entrada { get; private set; }

    public int Saida { get; private set; }

    public int CacheLeitura { get; private set; }

    public int CacheEscrita { get; private set; }

    public DateTimeOffset RegistradoEm { get; private set; }

    public int Total => Entrada + Saida + CacheLeitura + CacheEscrita;

    public static ConsumoDaIa Registrar(string origem, long conversaId, string modelo, int chamadas, int entrada, int saida, int cacheLeitura, int cacheEscrita, DateTimeOffset em)
    {
        if (new[] { chamadas, entrada, saida, cacheLeitura, cacheEscrita }.Any(v => v < 0))
        {
            throw new ValorInvalidoException("Consumo", "não pode ser negativo");
        }

        return new ConsumoDaIa
        {
            Origem = Guard.Obrigatorio(origem, nameof(Origem), OrigemMaxLength),
            ConversaId = Guard.IdValido(conversaId, nameof(ConversaId)),
            Modelo = Guard.Obrigatorio(modelo, nameof(Modelo), ModeloMaxLength),
            Chamadas = chamadas,
            Entrada = entrada,
            Saida = saida,
            CacheLeitura = cacheLeitura,
            CacheEscrita = cacheEscrita,
            RegistradoEm = em,
        };
    }
}
