using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Uma linha da composição: quantas unidades de qual peça (ex.: 12× elo oval, banho de ródio).
/// </summary>
public sealed class ItemComposicao : BaseEntity<long>
{
    public const int QuantidadeMaxima = 10_000;
    public const int ObservacaoMaxLength = 300;

    internal ItemComposicao(long pecaId, int quantidade, string? observacao)
    {
        PecaId = Guard.IdValido(pecaId, nameof(PecaId));
        Alterar(quantidade, observacao);
    }

    private ItemComposicao()
    {
    }

    public long PecaId { get; private set; }

    public int Quantidade { get; private set; }

    public string? Observacao { get; private set; }

    internal void Alterar(int quantidade, string? observacao)
    {
        Quantidade = quantidade is >= 1 and <= QuantidadeMaxima
            ? quantidade
            : throw new ValorInvalidoException(nameof(Quantidade), $"deve estar entre 1 e {QuantidadeMaxima}");
        Observacao = Guard.Opcional(observacao, nameof(Observacao), ObservacaoMaxLength);
    }
}
