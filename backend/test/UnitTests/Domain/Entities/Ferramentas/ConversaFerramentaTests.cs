using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Ferramentas;

public class ConversaFerramentaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 6, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveDesfazerSoAUltimaAcaoAindaValendo()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        var primeira = conversa.RegistrarMensagem(PapelMensagem.Assistente, "Desenhei a frente.", "{}", Agora, acao: new AcaoDaIa("Desenhei a frente", 0));
        var segunda = conversa.RegistrarMensagem(PapelMensagem.Assistente, "Desenhei a lateral.", "{}", Agora, acao: new AcaoDaIa("Desenhei a lateral", 11));

        var desfazerAntiga = () => conversa.DesfazerAcao(primeira.Ordem);
        desfazerAntiga.Should().Throw<RegraDeNegocioException>().WithMessage("*mais recentes*");

        conversa.DesfazerAcao(segunda.Ordem).Should().Be(11);
        segunda.AcaoDesfeita.Should().BeTrue();
        conversa.DesfazerAcao(primeira.Ordem).Should().Be(0, "a primeira ação criou o arquivo");
    }

    [Test]
    public void DeveRecusarDesfazerMensagemSemAcao()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        var fala = conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Oi", "{}", Agora);

        var desfazer = () => conversa.DesfazerAcao(fala.Ordem);

        desfazer.Should().Throw<RegraDeNegocioException>();
    }

    [Test]
    public void DeveApagarAsMensagensERecomecarANumeracao()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Oi", "{}", Agora);

        conversa.Apagar();
        var nova = conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "De novo", "{}", Agora);

        conversa.Mensagens.Should().ContainSingle();
        nova.Ordem.Should().Be(1);
    }

    [Test]
    public void DeveFicarAguardandoAIaEnquantoAFalaNaoTemResposta()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        var fala = conversa.RegistrarFala("Desenhe o anel", [], Agora);

        conversa.AguardandoIa(Agora.AddMinutes(5)).Should().BeTrue();
        var outra = () => conversa.RegistrarFala("E agora?", [], Agora.AddMinutes(5));
        outra.Should().Throw<RegraDeNegocioException>().WithMessage("*trabalhando*");

        conversa.ConfirmarFala(fala, "{\"blocos\":[{\"tipo\":\"texto\"}]}");
        conversa.RegistrarMensagem(PapelMensagem.Assistente, "Pronto.", "{}", Agora.AddMinutes(6));
        conversa.AguardandoIa(Agora.AddMinutes(6)).Should().BeFalse();
        fala.Conteudo.Should().Contain("texto");
    }

    [Test]
    public void DeveDescartarAFalaOrfaQuandoOTurnoExpirou()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        conversa.RegistrarFala("Ficou sem resposta", [], Agora);
        var depois = Agora + ConversaFerramenta.TempoMaximoDoTurno;

        conversa.AguardandoIa(depois).Should().BeFalse();
        var nova = conversa.RegistrarFala("De novo", [], depois);

        conversa.Mensagens.Should().ContainSingle().Which.Should().Be(nova);
        nova.Ordem.Should().Be(1);
    }

    [Test]
    public void DeveTirarAFalaQuandoAIaFalha()
    {
        var conversa = ConversaFerramenta.Criar(5, "desenho-2d", Agora);
        var fala = conversa.RegistrarFala("falhe", [], Agora);

        conversa.DescartarFala(fala);

        conversa.Mensagens.Should().BeEmpty();
        conversa.AguardandoIa(Agora).Should().BeFalse();
    }

    [Test]
    public void DeveRecusarArquivoVazio()
    {
        var criar = () => ArquivoFerramenta.Criar(3, [], OrigemArquivo.Ia, Agora);

        criar.Should().Throw<ValorInvalidoException>();
    }
}
