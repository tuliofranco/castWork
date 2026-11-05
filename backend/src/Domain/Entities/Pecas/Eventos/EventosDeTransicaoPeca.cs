namespace Castwork.Domain.Entities.Pecas.Eventos;

// Família de eventos de transição da peça, agrupada num arquivo porque cada um é uma linha
// e só faz sentido lido junto com a máquina de estados (TransicoesPeca).
public sealed record PecaEnviadaParaOrcamentoEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Orcamento, OcorridoEm);

public sealed record PecaVoltouParaBriefingEvent(Peca Peca, StatusPeca De, string Motivo, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Briefing, OcorridoEm);

public sealed record PecaAprovadaEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Aprovado, OcorridoEm);

public sealed record PecaEntrouEmProducaoEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Producao, OcorridoEm);

public sealed record PecaEntrouEmAcabamentoEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Acabamento, OcorridoEm);

public sealed record PecaRetornouParaProducaoEvent(Peca Peca, StatusPeca De, string Motivo, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Producao, OcorridoEm);

public sealed record PecaEntregueEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Entregue, OcorridoEm);

public sealed record PecaEntrouEmPosVendaEvent(Peca Peca, StatusPeca De, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.PosVenda, OcorridoEm);

public sealed record PecaCanceladaEvent(Peca Peca, StatusPeca De, string Motivo, DateTimeOffset OcorridoEm)
    : StatusPecaAlteradoEvent(Peca, De, StatusPeca.Cancelada, OcorridoEm);
