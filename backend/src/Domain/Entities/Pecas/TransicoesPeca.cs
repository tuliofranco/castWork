using System.Collections.Frozen;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Tabela da máquina de estados da peça: a única fonte de verdade sobre quais transições existem.
/// É pública para a Application expor ao front os próximos status possíveis (ex.: botões habilitados).
/// </summary>
public static class TransicoesPeca
{
    private static readonly FrozenDictionary<StatusPeca, FrozenSet<StatusPeca>> Permitidas =
        new Dictionary<StatusPeca, FrozenSet<StatusPeca>>
        {
            [StatusPeca.Briefing] = Set(StatusPeca.Orcamento, StatusPeca.Cancelada),
            [StatusPeca.Orcamento] = Set(StatusPeca.Aprovado, StatusPeca.Briefing, StatusPeca.Cancelada),
            [StatusPeca.Aprovado] = Set(StatusPeca.Producao, StatusPeca.Cancelada),
            [StatusPeca.Producao] = Set(StatusPeca.Acabamento, StatusPeca.Cancelada),
            [StatusPeca.Acabamento] = Set(StatusPeca.Entregue, StatusPeca.Producao, StatusPeca.Cancelada),
            [StatusPeca.Entregue] = Set(StatusPeca.PosVenda),
            [StatusPeca.PosVenda] = Set(),
            [StatusPeca.Cancelada] = Set(),
        }.ToFrozenDictionary();

    public static IReadOnlySet<StatusPeca> ProximosDe(StatusPeca status) => Permitidas[status];

    public static bool Permite(StatusPeca de, StatusPeca para) => Permitidas[de].Contains(para);

    /// <summary>
    /// Peça encerrada não aceita mais edição de dados (só observações de pós-venda).
    /// </summary>
    public static bool EstaEncerrada(StatusPeca status)
        => status is StatusPeca.Entregue or StatusPeca.PosVenda or StatusPeca.Cancelada;

    private static FrozenSet<StatusPeca> Set(params StatusPeca[] status) => status.ToFrozenSet();
}
