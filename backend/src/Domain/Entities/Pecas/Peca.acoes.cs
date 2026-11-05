using Castwork.Domain.Common;
using Castwork.Domain.Entities.Pecas.Eventos;
using Castwork.Domain.Exceptions;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Ações de negócio da <see cref="Peca"/>: criação, edição, observações e as transições da máquina de estados.
/// O estado (propriedades, limites e dados calculados) fica em <c>Peca.cs</c>.
/// </summary>
public sealed partial class Peca
{
    public static Peca Criar(long clienteId, string titulo, string? descricao, DateOnly? prazoEntrega, DateTimeOffset agora)
    {
        var peca = new Peca
        {
            ClienteId = Guard.IdValido(clienteId, nameof(ClienteId)),
            Status = StatusPeca.Briefing,
        };

        peca.DefinirDados(titulo, descricao, prazoEntrega);
        peca._etapas.Add(new EtapaPeca(StatusPeca.Briefing, agora));
        peca.AdicionarEvento(new PecaCriadaEvent(peca, agora));
        return peca;
    }

    public void AtualizarDados(string titulo, string? descricao, DateOnly? prazoEntrega)
    {
        GarantirNaoEncerrada();
        DefinirDados(titulo, descricao, prazoEntrega);
    }

    public void AdicionarObservacao(string texto, DateTimeOffset agora)
    {
        if (Status == StatusPeca.Cancelada)
        {
            throw new RegraDeNegocioException("Não é possível adicionar observações a uma peça cancelada.");
        }

        _observacoes.Add(new ObservacaoPeca(texto, Status, agora));
    }

    public void EnviarParaOrcamento(DateTimeOffset agora)
        => TransicionarPara(StatusPeca.Orcamento, agora, de => new PecaEnviadaParaOrcamentoEvent(this, de, agora));

    public void VoltarParaBriefing(string motivo, DateTimeOffset agora)
    {
        var motivoValido = Guard.Obrigatorio(motivo, "Motivo", MotivoMaxLength);
        TransicionarPara(StatusPeca.Briefing, agora, de => new PecaVoltouParaBriefingEvent(this, de, motivoValido, agora));
    }

    public void Aprovar(DateTimeOffset agora)
        => TransicionarPara(StatusPeca.Aprovado, agora, de => new PecaAprovadaEvent(this, de, agora));

    public void IniciarProducao(DateTimeOffset agora)
        => TransicionarPara(StatusPeca.Producao, agora, de => new PecaEntrouEmProducaoEvent(this, de, agora));

    public void EnviarParaAcabamento(DateTimeOffset agora)
        => TransicionarPara(StatusPeca.Acabamento, agora, de => new PecaEntrouEmAcabamentoEvent(this, de, agora));

    /// <summary>
    /// Retrabalho: a peça volta do acabamento para a bancada.
    /// </summary>
    public void RetornarParaProducao(string motivo, DateTimeOffset agora)
    {
        var motivoValido = Guard.Obrigatorio(motivo, "Motivo", MotivoMaxLength);
        TransicionarPara(StatusPeca.Producao, agora, de => new PecaRetornouParaProducaoEvent(this, de, motivoValido, agora));
    }

    public void Entregar(DateTimeOffset agora)
    {
        TransicionarPara(StatusPeca.Entregue, agora, de => new PecaEntregueEvent(this, de, agora));
        EntregueEm = agora;
    }

    public void IniciarPosVenda(DateTimeOffset agora)
        => TransicionarPara(StatusPeca.PosVenda, agora, de => new PecaEntrouEmPosVendaEvent(this, de, agora));

    public void Cancelar(string motivo, DateTimeOffset agora)
    {
        var motivoValido = Guard.Obrigatorio(motivo, "Motivo", MotivoMaxLength);
        TransicionarPara(StatusPeca.Cancelada, agora, de => new PecaCanceladaEvent(this, de, motivoValido, agora));
        MotivoCancelamento = motivoValido;
    }

    private void TransicionarPara(StatusPeca novo, DateTimeOffset agora, Func<StatusPeca, StatusPecaAlteradoEvent> criarEvento)
    {
        if (!TransicoesPeca.Permite(Status, novo))
        {
            throw new TransicaoDeStatusInvalidaException(nameof(Peca), Status, novo);
        }

        var anterior = Status;
        EtapaAtual?.Concluir(agora);
        Status = novo;

        // Entregue e Cancelada são marcos instantâneos: a etapa abre e fecha no mesmo momento.
        var etapa = new EtapaPeca(novo, agora);
        if (novo is StatusPeca.Entregue or StatusPeca.Cancelada)
        {
            etapa.Concluir(agora);
        }

        _etapas.Add(etapa);
        AdicionarEvento(criarEvento(anterior));
    }

    private void DefinirDados(string titulo, string? descricao, DateOnly? prazoEntrega)
    {
        Titulo = Guard.Obrigatorio(titulo, nameof(Titulo), TituloMaxLength);
        Descricao = Guard.Opcional(descricao, nameof(Descricao), DescricaoMaxLength);
        PrazoEntrega = prazoEntrega;
    }

    private void GarantirNaoEncerrada()
    {
        if (EstaEncerrada)
        {
            throw new RegraDeNegocioException($"A peça está '{Status}' e não pode mais ser alterada.");
        }
    }
}
