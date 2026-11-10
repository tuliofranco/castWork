using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

namespace Castwork.DomainService.Precificacao;

/// <summary>
/// Tudo o que entra no preço de uma versão, já lido do banco: os preços do projeto, os metais com peso, a composição
/// com o custo de cada peça, a mão de obra, os serviços e o markup da versão. <see cref="Agora"/> só decide os avisos.
/// <see cref="Modelo"/>: o modelo 3D medido (os <see cref="Metais"/> já vêm com o peso dele); nulo quando a versão não tem
/// 3D e vale o peso manual.
/// </summary>
public sealed record EntradaDoPreco(
    ParametrosDePreco Parametros,
    IReadOnlyList<CotacaoUsada> Cotacoes,
    IReadOnlyList<MetalUsado> Metais,
    IReadOnlyList<PecaUsada> Composicao,
    MaoDeObra? MaoDeObra,
    IReadOnlyList<ServicoExterno> Servicos,
    decimal? MarkupDaVersao,
    DateTimeOffset Agora,
    ModeloMedido? Modelo = null);

/// <summary>
/// Cotação do metal puro no projeto (R$/g) e a data de origem dela.
/// </summary>
public sealed record CotacaoUsada(Metal Metal, Dinheiro Valor, DateTimeOffset CotadaEm);

/// <summary>
/// Uma liga da versão com os dados da liga (metal, teor, se ainda está ativa) e o peso da peça: o medido no modelo 3D ou,
/// sem 3D, o estimado manualmente.
/// </summary>
public sealed record MetalUsado(long LigaId, string Liga, Metal Metal, TeorMetal Teor, bool LigaAtiva, Peso PesoEstimado, Peso? PesoReal);

/// <summary>
/// Uma linha da composição com o custo unitário da peça na biblioteca (nulo: peça sem custo).
/// </summary>
public sealed record PecaUsada(long PecaId, string Nome, int Quantidade, Dinheiro? CustoUnitario);
