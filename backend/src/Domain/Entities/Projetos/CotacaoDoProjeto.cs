using Castwork.Domain.Common;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Cotação de um metal puro (R$/g) usada nos orçamentos do projeto. Nasce copiada da vigente do ateliê e pode ser
/// editada só no projeto: o ateliê atualizar o preço do ouro não muda o orçamento de um cliente.
/// </summary>
public sealed class CotacaoDoProjeto : BaseEntity<long>
{
    internal CotacaoDoProjeto(Metal metal, Dinheiro valor, DateTimeOffset cotadaEm)
    {
        Metal = Enum.IsDefined(metal) ? metal : throw new ValorInvalidoException(nameof(Metal), "não é um metal válido");
        Alterar(valor, cotadaEm);
    }

    private CotacaoDoProjeto()
    {
    }

    public Metal Metal { get; private set; }

    /// <summary>
    /// R$ por grama do metal puro (24k / prata fina), nunca da liga.
    /// </summary>
    public Dinheiro Valor { get; private set; } = Dinheiro.Zero;

    /// <summary>
    /// Data de origem do valor: quando o ateliê registrou a cotação herdada, ou quando o projeto a editou.
    /// </summary>
    public DateTimeOffset CotadaEm { get; private set; }

    internal void Alterar(Dinheiro valor, DateTimeOffset cotadaEm)
    {
        Valor = valor > Dinheiro.Zero ? valor : throw new ValorInvalidoException(nameof(Valor), "deve ser maior que zero");
        CotadaEm = cotadaEm;
    }
}
