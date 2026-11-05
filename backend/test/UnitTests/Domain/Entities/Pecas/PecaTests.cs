using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Pecas.Eventos;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Pecas;

public class PecaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 0, TimeSpan.FromHours(-3));

    private static readonly StatusPeca[] CaminhoFeliz =
    [
        StatusPeca.Orcamento, StatusPeca.Aprovado, StatusPeca.Producao,
        StatusPeca.Acabamento, StatusPeca.Entregue, StatusPeca.PosVenda,
    ];

    [Test]
    public void DeveCriarPecaEmBriefingComEtapaAbertaEEvento()
    {
        var peca = Peca.Criar(7, "  Anel solitário  ", "Ouro 18k com diamante", new DateOnly(2026, 12, 1), Agora);

        peca.Status.Should().Be(StatusPeca.Briefing);
        peca.ClienteId.Should().Be(7);
        peca.Titulo.Should().Be("Anel solitário");
        peca.Etapas.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Status = StatusPeca.Briefing, IniciadaEm = Agora, ConcluidaEm = (DateTimeOffset?)null });
        peca.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PecaCriadaEvent>().Which.Peca.Should().BeSameAs(peca);
    }

    [TestCase(0, "Anel")]
    [TestCase(1, "")]
    [TestCase(1, "   ")]
    public void DeveRejeitarCriacaoDePecaComDadosInvalidos(long clienteId, string titulo)
    {
        var criar = () => Peca.Criar(clienteId, titulo, null, null, Agora);

        criar.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DevePercorrerCaminhoFelizRegistrandoEtapasEEventos()
    {
        var peca = NovaPeca();

        LevarAte(peca, StatusPeca.PosVenda);

        peca.Status.Should().Be(StatusPeca.PosVenda);
        peca.EntregueEm.Should().Be(Agora.AddHours(5));
        peca.Etapas.Select(e => e.Status).Should().Equal([StatusPeca.Briefing, .. CaminhoFeliz]);
        peca.Etapas.Where(e => e.EmAndamento).Should().ContainSingle().Which.Status.Should().Be(StatusPeca.PosVenda);
        peca.DomainEvents.OfType<StatusPecaAlteradoEvent>().Select(e => (e.De, e.Para)).Should().Equal(
            (StatusPeca.Briefing, StatusPeca.Orcamento),
            (StatusPeca.Orcamento, StatusPeca.Aprovado),
            (StatusPeca.Aprovado, StatusPeca.Producao),
            (StatusPeca.Producao, StatusPeca.Acabamento),
            (StatusPeca.Acabamento, StatusPeca.Entregue),
            (StatusPeca.Entregue, StatusPeca.PosVenda));
    }

    [Test]
    public void DeveFecharEtapaAnteriorAoTransicionar()
    {
        var peca = NovaPeca();

        peca.EnviarParaOrcamento(Agora.AddDays(1));

        var briefing = peca.Etapas.Single(e => e.Status == StatusPeca.Briefing);
        briefing.ConcluidaEm.Should().Be(Agora.AddDays(1));
        peca.EtapaAtual!.Status.Should().Be(StatusPeca.Orcamento);
    }

    [TestCaseSource(nameof(TransicoesProibidas))]
    public void DeveLancarExcecaoAoTransicionarPecaParaStatusNaoPermitido(StatusPeca de, StatusPeca para)
    {
        var peca = NovaPeca();
        LevarAte(peca, de);

        var transicionar = () => Transicionar(peca, para);

        transicionar.Should().Throw<TransicaoDeStatusInvalidaException>()
            .Which.Should().BeEquivalentTo(new { De = de.ToString(), Para = para.ToString() });
    }

    [Test]
    public void DeveCancelarPecaRegistrandoMotivo()
    {
        var peca = NovaPeca();
        LevarAte(peca, StatusPeca.Producao);

        peca.Cancelar("Cliente desistiu", Agora.AddDays(3));

        peca.Status.Should().Be(StatusPeca.Cancelada);
        peca.MotivoCancelamento.Should().Be("Cliente desistiu");
        peca.EtapaAtual.Should().BeNull();
        peca.DomainEvents.Last().Should().BeOfType<PecaCanceladaEvent>()
            .Which.Should().BeEquivalentTo(new { De = StatusPeca.Producao, Para = StatusPeca.Cancelada, Motivo = "Cliente desistiu" });
    }

    [TestCase("")]
    [TestCase("  ")]
    public void DeveExigirMotivoAoCancelarPeca(string motivo)
    {
        var peca = NovaPeca();

        var cancelar = () => peca.Cancelar(motivo, Agora);

        cancelar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be("Motivo");
        peca.Status.Should().Be(StatusPeca.Briefing);
    }

    [Test]
    public void DeveRetornarPecaDoAcabamentoParaProducaoEmRetrabalho()
    {
        var peca = NovaPeca();
        LevarAte(peca, StatusPeca.Acabamento);

        peca.RetornarParaProducao("Garra solta", Agora.AddDays(4));

        peca.Status.Should().Be(StatusPeca.Producao);
        peca.Etapas.Count(e => e.Status == StatusPeca.Producao).Should().Be(2);
        peca.DomainEvents.Last().Should().BeOfType<PecaRetornouParaProducaoEvent>().Which.Motivo.Should().Be("Garra solta");
    }

    [TestCase(StatusPeca.Entregue)]
    [TestCase(StatusPeca.PosVenda)]
    [TestCase(StatusPeca.Cancelada)]
    public void DeveImpedirAlteracaoDeDadosDePecaEncerrada(StatusPeca status)
    {
        var peca = NovaPeca();
        LevarAte(peca, status);

        var atualizar = () => peca.AtualizarDados("Outro título", null, null);

        atualizar.Should().Throw<RegraDeNegocioException>();
    }

    [Test]
    public void DeveAdicionarObservacaoComStatusAtualDaPeca()
    {
        var peca = NovaPeca();
        LevarAte(peca, StatusPeca.Aprovado);

        peca.AdicionarObservacao("Cliente pediu aro 16", Agora);

        peca.Observacoes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Texto = "Cliente pediu aro 16", StatusDaPeca = StatusPeca.Aprovado, CriadaEm = Agora });
    }

    private static Peca NovaPeca() => Peca.Criar(1, "Anel", null, null, Agora);

    /// <summary>
    /// Leva a peça até o status pedido pelo caminho mais curto (cancelada sai direto do briefing).
    /// </summary>
    private static void LevarAte(Peca peca, StatusPeca alvo)
    {
        if (alvo == StatusPeca.Cancelada)
        {
            peca.Cancelar("teste", Agora);
            return;
        }

        foreach (var (status, i) in CaminhoFeliz.Select((s, i) => (s, i)))
        {
            if (peca.Status == alvo)
            {
                return;
            }

            Transicionar(peca, status, Agora.AddHours(i + 1));
        }
    }

    private static void Transicionar(Peca peca, StatusPeca para, DateTimeOffset? em = null)
    {
        var agora = em ?? Agora;
        switch (para)
        {
            case StatusPeca.Briefing: peca.VoltarParaBriefing("motivo", agora); break;
            case StatusPeca.Orcamento: peca.EnviarParaOrcamento(agora); break;
            case StatusPeca.Aprovado: peca.Aprovar(agora); break;
            case StatusPeca.Producao when peca.Status == StatusPeca.Acabamento: peca.RetornarParaProducao("motivo", agora); break;
            case StatusPeca.Producao: peca.IniciarProducao(agora); break;
            case StatusPeca.Acabamento: peca.EnviarParaAcabamento(agora); break;
            case StatusPeca.Entregue: peca.Entregar(agora); break;
            case StatusPeca.PosVenda: peca.IniciarPosVenda(agora); break;
            case StatusPeca.Cancelada: peca.Cancelar("motivo", agora); break;
            default: throw new ArgumentOutOfRangeException(nameof(para));
        }
    }

    private static IEnumerable<TestCaseData> TransicoesProibidas()
        => from de in Enum.GetValues<StatusPeca>()
           from para in Enum.GetValues<StatusPeca>()
           where !TransicoesPeca.Permite(de, para)
           select new TestCaseData(de, para);
}
