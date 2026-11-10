using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

namespace Castwork.DomainService.Precificacao;

/// <summary>
/// O preço da versão linha a linha: metais, composição, mão de obra e serviços somam o custo; o preço é o custo ×
/// markup × (1 + taxas), arredondado para cima. Incompleto enquanto houver pendência (o preço sai assim mesmo, parcial).
/// </summary>
public sealed record PrecoCalculado(
    IReadOnlyList<CustoDoMetal> Metais,
    Dinheiro CustoDosMetais,
    IReadOnlyList<CustoDaPeca> Composicao,
    Dinheiro CustoDaComposicao,
    CustoDaMaoDeObra MaoDeObra,
    IReadOnlyList<ServicoExterno> Servicos,
    Dinheiro CustoDosServicos,
    Dinheiro CustoTotal,
    decimal PerdaPercentual,
    decimal Markup,
    bool MarkupDaVersao,
    decimal TaxasPercentual,
    Dinheiro ArredondarPara,
    Dinheiro PrecoSemArredondar,
    Dinheiro Preco,
    StatusDoPreco Status,
    IReadOnlyList<PendenciaDoPreco> Pendencias,
    IReadOnlyList<AvisoDoPreco> Avisos);

/// <summary>
/// Uma liga: peso da peça → peso a fundir (com a perda) → custo = peso a fundir × cotação do puro × pureza.
/// Sem cotação do metal, <see cref="CotacaoDoPuro"/> e <see cref="ValorPorGrama"/> ficam nulos e o custo é zero.
/// </summary>
public sealed record CustoDoMetal(
    long LigaId,
    string Liga,
    Metal Metal,
    TeorMetal Teor,
    bool LigaAtiva,
    Peso PesoEstimado,
    Peso PesoAFundir,
    Dinheiro? CotacaoDoPuro,
    Dinheiro? ValorPorGrama,
    Dinheiro Custo,
    Peso? PesoReal,
    decimal? DiferencaDoPesoReal);

/// <summary>
/// Uma peça da composição: quantidade × custo unitário (zero quando a peça não tem custo).
/// </summary>
public sealed record CustoDaPeca(long PecaId, string Nome, int Quantidade, Dinheiro? CustoUnitario, Dinheiro Custo);

/// <summary>
/// Mão de obra: por horas (com o valor da hora do projeto) ou valor fixo. <see cref="Tipo"/> nulo: não informada.
/// </summary>
public sealed record CustoDaMaoDeObra(TipoMaoDeObra? Tipo, decimal? Horas, Dinheiro? ValorHora, Dinheiro Custo);

/// <summary>
/// O que deixa o preço incompleto, com a referência para resolver (a peça, a liga ou o metal).
/// </summary>
public sealed record PendenciaDoPreco(TipoDePendencia Tipo, string Descricao, long? PecaId = null, long? LigaId = null, Metal? Metal = null);

/// <summary>
/// Atenção que não bloqueia o preço (ex.: cotação antiga).
/// </summary>
public sealed record AvisoDoPreco(TipoDeAviso Tipo, string Descricao, Metal? Metal = null);
