using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

namespace Castwork.DomainService.Precificacao;

/// <summary>
/// Tudo o que entra no preço de uma versão, já lido do banco: os preços do projeto, os metais com peso, a composição
/// com o custo de cada peça, a mão de obra, os serviços e o markup da versão. <see cref="Agora"/> só decide os avisos.
/// </summary>
public sealed record EntradaDoPreco(
    ParametrosDePreco Parametros,
    IReadOnlyList<CotacaoUsada> Cotacoes,
    IReadOnlyList<MetalUsado> Metais,
    IReadOnlyList<PecaUsada> Composicao,
    MaoDeObra? MaoDeObra,
    IReadOnlyList<ServicoExterno> Servicos,
    decimal? MarkupDaVersao,
    DateTimeOffset Agora);

/// <summary>
/// Cotação do metal puro no projeto (R$/g) e a data de origem dela.
/// </summary>
public sealed record CotacaoUsada(Metal Metal, Dinheiro Valor, DateTimeOffset CotadaEm);

/// <summary>
/// Uma liga da versão com os dados da liga (metal, teor, se ainda está ativa) e o peso da peça.
/// </summary>
public sealed record MetalUsado(long LigaId, string Liga, Metal Metal, TeorMetal Teor, bool LigaAtiva, Peso PesoEstimado, Peso? PesoReal);

/// <summary>
/// Uma linha da composição com o custo unitário da peça na biblioteca (nulo: peça sem custo).
/// </summary>
public sealed record PecaUsada(long PecaId, string Nome, int Quantidade, Dinheiro? CustoUnitario);
