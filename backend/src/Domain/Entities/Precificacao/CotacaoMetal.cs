using Castwork.Domain.Common;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Cotação de um metal puro em R$/g, registrada pelo ateliê. Cada atualização é uma linha nova e nada é
/// sobrescrito: a vigente é a mais recente e as anteriores formam o histórico.
/// </summary>
public sealed partial class CotacaoMetal : BaseEntity<long>, IAggregateRoot
{
    private CotacaoMetal()
    {
    }

    public Metal Metal { get; private set; }

    /// <summary>
    /// R$ por grama do metal puro (24k / prata fina), nunca da liga.
    /// </summary>
    public Dinheiro Valor { get; private set; } = Dinheiro.Zero;

    public DateTimeOffset RegistradaEm { get; private set; }

    /// <summary>
    /// Usuário que registrou. Nulo fora de uma requisição.
    /// </summary>
    public long? RegistradaPor { get; private set; }
}
