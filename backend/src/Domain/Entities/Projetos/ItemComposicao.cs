using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Uma linha da composição: quantas unidades de qual peça do projeto (ex.: 12× elo oval, banho de ródio).
/// </summary>
public sealed class ItemComposicao : BaseEntity<long>
{
    public const int QuantidadeMaxima = 10_000;
    public const int ObservacaoMaxLength = 300;

    internal ItemComposicao(long pecaDoProjetoId, int quantidade, string? observacao)
    {
        PecaDoProjetoId = Guard.IdValido(pecaDoProjetoId, nameof(PecaDoProjetoId));
        Alterar(quantidade, observacao);
    }

    private ItemComposicao()
    {
    }

    /// <summary>
    /// A peça do mesmo projeto (<see cref="PecaDoProjeto"/>), nunca a da biblioteca.
    /// </summary>
    public long PecaDoProjetoId { get; private set; }

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
