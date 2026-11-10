using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.DomainService.Precificacao;

namespace Castwork.Application.Projetos.Queries.GetPrecoDaVersao;

/// <summary>
/// Preço da versão, calculado na leitura. Preço e status são para todos que leem a versão; o cálculo (custos,
/// markup, cotações, pendências e avisos) só sai para quem tem <c>precificacao.ler</c>.
/// </summary>
public sealed class PrecoDaVersaoDto
{
    public long VersaoId { get; init; }

    /// <summary>
    /// Preço final (R$), já arredondado. Parcial quando o status é incompleto.
    /// </summary>
    public decimal Preco { get; init; }

    public StatusDoPreco Status { get; init; }

    /// <summary>
    /// Nulo sem <c>precificacao.ler</c>.
    /// </summary>
    public CalculoDoPrecoDto? Calculo { get; init; }
}

/// <summary>
/// O cálculo linha a linha e os dados do orçamento da versão (para editar).
/// </summary>
public sealed class CalculoDoPrecoDto
{
    public IReadOnlyList<CustoDoMetalDto> Metais { get; init; } = [];

    public decimal CustoDosMetais { get; init; }

    public IReadOnlyList<CustoDaPecaDto> Composicao { get; init; } = [];

    public decimal CustoDaComposicao { get; init; }

    public CustoDaMaoDeObraDto MaoDeObra { get; init; } = new();

    public IReadOnlyList<ServicoExternoDto> Servicos { get; init; } = [];

    public decimal CustoDosServicos { get; init; }

    public decimal CustoTotal { get; init; }

    /// <summary>
    /// % de perda do projeto, aplicada ao peso de cada liga.
    /// </summary>
    public decimal PerdaPercentual { get; init; }

    /// <summary>
    /// Markup aplicado: o da versão, ou o do projeto quando a versão não tem.
    /// </summary>
    public decimal Markup { get; init; }

    /// <summary>
    /// Markup próprio da versão; nulo quando vale o do projeto.
    /// </summary>
    public decimal? MarkupDaVersao { get; init; }

    public decimal TaxasPercentual { get; init; }

    public decimal ArredondarPara { get; init; }

    /// <summary>
    /// Custo × markup × (1 + taxas), antes de arredondar.
    /// </summary>
    public decimal PrecoSemArredondar { get; init; }

    public IReadOnlyList<PendenciaDoPrecoDto> Pendencias { get; init; } = [];

    public IReadOnlyList<AvisoDoPrecoDto> Avisos { get; init; } = [];
}

public sealed class CustoDoMetalDto
{
    public long LigaId { get; init; }

    public string Liga { get; init; } = string.Empty;

    public Metal Metal { get; init; }

    /// <summary>
    /// Milésimos (750 = 18k).
    /// </summary>
    public int Teor { get; init; }

    public bool LigaAtiva { get; init; }

    /// <summary>
    /// Peso estimado da peça pronta (g).
    /// </summary>
    public decimal PesoEstimado { get; init; }

    /// <summary>
    /// Peso estimado + perda (g).
    /// </summary>
    public decimal PesoAFundir { get; init; }

    /// <summary>
    /// R$/g do metal puro no projeto; nulo sem cotação.
    /// </summary>
    public decimal? CotacaoDoPuro { get; init; }

    /// <summary>
    /// R$/g da liga (cotação × pureza); nulo sem cotação.
    /// </summary>
    public decimal? ValorPorGrama { get; init; }

    public decimal Custo { get; init; }

    public decimal? PesoReal { get; init; }

    /// <summary>
    /// % do peso real sobre o estimado (+4 = 4% mais pesada).
    /// </summary>
    public decimal? DiferencaDoPesoReal { get; init; }
}

public sealed class CustoDaPecaDto
{
    public long PecaId { get; init; }

    public string Nome { get; init; } = string.Empty;

    public int Quantidade { get; init; }

    /// <summary>
    /// Nulo: peça sem custo na biblioteca.
    /// </summary>
    public decimal? CustoUnitario { get; init; }

    public decimal Custo { get; init; }
}

public sealed class CustoDaMaoDeObraDto
{
    /// <summary>
    /// Nulo enquanto não foi informada.
    /// </summary>
    public TipoMaoDeObra? Tipo { get; init; }

    public decimal? Horas { get; init; }

    /// <summary>
    /// Valor da hora do projeto, quando é por horas.
    /// </summary>
    public decimal? ValorHora { get; init; }

    public decimal Custo { get; init; }
}

public sealed class ServicoExternoDto
{
    public string Nome { get; init; } = string.Empty;

    public decimal Valor { get; init; }
}

public sealed class PendenciaDoPrecoDto
{
    public TipoDePendencia Tipo { get; init; }

    public string Descricao { get; init; } = string.Empty;

    /// <summary>
    /// A peça sem custo, para abrir e resolver.
    /// </summary>
    public long? PecaId { get; init; }

    /// <summary>
    /// A liga desativada.
    /// </summary>
    public long? LigaId { get; init; }

    /// <summary>
    /// O metal sem cotação no projeto.
    /// </summary>
    public Metal? Metal { get; init; }
}

public sealed class AvisoDoPrecoDto
{
    public TipoDeAviso Tipo { get; init; }

    public string Descricao { get; init; } = string.Empty;

    public Metal? Metal { get; init; }
}
