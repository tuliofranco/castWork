using System.Reflection;

using Castwork.Domain.Common;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Assistente;

public class ConversaFerramentaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 6, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveCriarAConversaDaPecaDoProjetoSemVersao()
    {
        var conversa = ConversaFerramenta.Criar(1, versaoId: null, "modelo-3d", Agora, pecaDoProjetoId: 9);

        conversa.Should().BeEquivalentTo(new { ProjetoId = 1L, VersaoId = (long?)null, PecaDoProjetoId = (long?)9, Ferramenta = "modelo-3d" });
    }

    [Test]
    public void DeveRecusarConversaDaVersaoEDaPecaAoMesmoTempo()
    {
        var criar = () => ConversaFerramenta.Criar(1, 5, "modelo-3d", Agora, pecaDoProjetoId: 9);

        criar.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveDesfazerSoAUltimaAcaoAindaValendo()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
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
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        var fala = conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Oi", "{}", Agora);

        var desfazer = () => conversa.DesfazerAcao(fala.Ordem);

        desfazer.Should().Throw<RegraDeNegocioException>();
    }

    [Test]
    public void DeveApagarAsMensagensERecomecarANumeracao()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Oi", "{}", Agora);

        conversa.Apagar();
        var nova = conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "De novo", "{}", Agora);

        conversa.Mensagens.Should().ContainSingle();
        nova.Ordem.Should().Be(1);
    }

    [Test]
    public void DeveFicarAguardandoAIaEnquantoAFalaNaoTemResposta()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
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
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
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
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
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

    [Test]
    public void DeveComecarUmChatNovoGuardandoOAnterior()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Primeiro", "{}", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Assistente, "Ok", "{}", Agora);

        conversa.NovoChat(Agora);
        var nova = conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Segundo", "{}", Agora);

        conversa.ChatAtual.Should().Be(2);
        nova.Ordem.Should().Be(3, "a ordem é única na conversa toda");
        conversa.MensagensDoChat.Select(m => m.Texto).Should().Equal("Segundo");
        conversa.Mensagens.Should().HaveCount(3);
    }

    [Test]
    public void DeveIgnorarNovoChatComOAtualVazio()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);

        conversa.NovoChat(Agora);

        conversa.ChatAtual.Should().Be(1);
    }

    [Test]
    public void DeveRetomarUmChatAnteriorEApagarSoOEmAndamento()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Primeiro", "{}", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Assistente, "Ok", "{}", Agora);
        conversa.NovoChat(Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Segundo", "{}", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Assistente, "Ok", "{}", Agora);

        conversa.ContinuarChat(1, Agora);
        conversa.Apagar();

        conversa.Mensagens.Should().OnlyContain(m => m.Chat == 2);
    }

    [Test]
    public void DeveRecusarTrocarDeChatComAIaTrabalhando()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        conversa.RegistrarFala("Desenhe", [], Agora);

        var novo = () => conversa.NovoChat(Agora.AddSeconds(5));

        novo.Should().Throw<RegraDeNegocioException>();
    }

    [Test]
    public void DeveRecusarDesfazerAcaoDeOutroChat()
    {
        var conversa = ConversaFerramenta.Criar(1, 5, "desenho-2d", Agora);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Desenhe", "{}", Agora);
        var acao = conversa.RegistrarMensagem(PapelMensagem.Assistente, "Feito", "{}", Agora, acao: new AcaoDaIa("Desenhou", 0));
        conversa.NovoChat(Agora);

        var desfazer = () => conversa.DesfazerAcao(acao.Ordem);

        desfazer.Should().Throw<RegraDeNegocioException>().WithMessage("*chat em andamento*");
    }

    [Test]
    public void DeveMarcarOArquivoFinalSoNaPecaESoUmaVez()
    {
        var conversa = ComId(ConversaFerramenta.Criar(1, versaoId: null, "modelo-3d", Agora, pecaDoProjetoId: 9), 3);
        var arquivo = ComId(ArquivoFerramenta.Criar(3, [1], OrigemArquivo.Joalheiro, Agora), 20);

        conversa.MarcarArquivoFinal(arquivo).Should().BeTrue();
        conversa.MarcarArquivoFinal(arquivo).Should().BeFalse("o final já é este");
        conversa.ArquivoFinalId.Should().Be(20);

        var daVersao = ComId(ConversaFerramenta.Criar(1, 5, "modelo-3d", Agora), 3);
        var naVersao = () => daVersao.MarcarArquivoFinal(arquivo);
        naVersao.Should().Throw<RegraDeNegocioException>().WithMessage("*peça do projeto*");
    }

    [Test]
    public void DeveRecusarComoFinalOArquivoDeOutraConversaArquivadoOuEmBranco()
    {
        var conversa = ComId(ConversaFerramenta.Criar(1, versaoId: null, "desenho-2d", Agora, pecaDoProjetoId: 9), 3);
        var deOutra = ComId(ArquivoFerramenta.Criar(4, [1], OrigemArquivo.Joalheiro, Agora), 20);
        var arquivado = ComId(ArquivoFerramenta.Criar(3, [1], OrigemArquivo.Joalheiro, Agora), 21);
        arquivado.Arquivar(Agora);
        var emBranco = ComId(ArquivoFerramenta.EmBrancoPara(3, Agora), 22);

        ((Action)(() => conversa.MarcarArquivoFinal(deOutra))).Should().Throw<RegraDeNegocioException>().WithMessage("*não é desta conversa*");
        ((Action)(() => conversa.MarcarArquivoFinal(arquivado))).Should().Throw<RegraDeNegocioException>().WithMessage("*Desarquive*");
        ((Action)(() => conversa.MarcarArquivoFinal(emBranco))).Should().Throw<RegraDeNegocioException>().WithMessage("*em branco*");
        conversa.ArquivoFinalId.Should().BeNull();
    }

    [Test]
    public void DeveRecusarArquivarOArquivoFinal()
    {
        var conversa = ComId(ConversaFerramenta.Criar(1, versaoId: null, "desenho-2d", Agora, pecaDoProjetoId: 9), 3);
        var final = ComId(ArquivoFerramenta.Criar(3, [1], OrigemArquivo.Joalheiro, Agora), 20);
        var outro = ComId(ArquivoFerramenta.Criar(3, [2], OrigemArquivo.Joalheiro, Agora), 21);
        conversa.MarcarArquivoFinal(final);

        ((Action)(() => conversa.GarantirQuePodeArquivar(final))).Should().Throw<RegraDeNegocioException>().WithMessage("*arquivo final*");
        ((Action)(() => conversa.GarantirQuePodeArquivar(outro))).Should().NotThrow();
    }

    [Test]
    public void DeveRecusarNovoChatNaPeca()
    {
        var conversa = ConversaFerramenta.Criar(1, versaoId: null, "modelo-3d", Agora, pecaDoProjetoId: 9);
        conversa.RegistrarMensagem(PapelMensagem.Joalheiro, "Oi", "{}", Agora.AddHours(-1));

        var novoChat = () => conversa.NovoChat(Agora);

        novoChat.Should().Throw<RegraDeNegocioException>().WithMessage("*uma conversa só*");
        conversa.ChatAtual.Should().Be(1);
    }

    private static T ComId<T>(T entidade, long id)
        where T : BaseEntity<long>
    {
        typeof(BaseEntity<long>).GetProperty(nameof(BaseEntity<long>.Id))!.SetValue(entidade, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        return entidade;
    }
}
