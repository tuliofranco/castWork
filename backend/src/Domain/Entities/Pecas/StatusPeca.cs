namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Pipeline da peça. Os valores numéricos são estáveis, mas o banco grava um código próprio (char) via converter;
/// o domínio não sabe disso.
/// </summary>
public enum StatusPeca
{
    Briefing = 1,
    Orcamento = 2,
    Aprovado = 3,
    Producao = 4,
    Acabamento = 5,
    Entregue = 6,
    PosVenda = 7,
    Cancelada = 99,
}
