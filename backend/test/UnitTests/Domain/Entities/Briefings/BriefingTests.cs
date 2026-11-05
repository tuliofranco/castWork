using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Briefings;

public class BriefingTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void DeveCriarBriefingComOFormularioSemConversa()
    {
        var briefing = Briefing.Criar(7, Formulario(medidas: "  aro 16  "));

        briefing.Should().BeEquivalentTo(new
        {
            ProjetoId = 7L,
            TipoJoia = TipoJoia.Anel,
            OQueOClienteQuer = "Anel de noivado delicado",
            Medidas = "aro 16",
            Prazo = (DateOnly?)new DateOnly(2026, 12, 20),
            OQueSeraFeito = (string?)null,
        });
        briefing.Mensagens.Should().BeEmpty();
    }

    [TestCase(" ")]
    [TestCase("")]
    public void DeveExigirOQueOClienteQuer(string pedido)
    {
        var criar = () => Briefing.Criar(7, Formulario() with { OQueOClienteQuer = pedido });

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(Briefing.OQueOClienteQuer));
    }

    [Test]
    public void DeveRecusarTipoDeJoiaInexistente()
    {
        var criar = () => Briefing.Criar(7, Formulario() with { TipoJoia = (TipoJoia)42 });

        criar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(Briefing.TipoJoia));
    }

    [Test]
    public void DeveRefinarSoOsCamposInformados()
    {
        var briefing = Briefing.Criar(7, Formulario(medidas: "aro 16"));

        briefing.Refinar(new RefinamentoBriefing(OQueSeraFeito: "Solitário em ouro 18k", Pedras: "Diamante 0,3 ct"));

        briefing.Should().BeEquivalentTo(new
        {
            OQueSeraFeito = "Solitário em ouro 18k",
            Pedras = "Diamante 0,3 ct",
            Medidas = "aro 16",
            OQueOClienteQuer = "Anel de noivado delicado",
        });
    }

    [Test]
    public void DeveCorrigirOFormularioSemPerderORefinamento()
    {
        var briefing = Briefing.Criar(7, Formulario());
        briefing.Refinar(new RefinamentoBriefing(Fecho: "Gaveta"));

        briefing.PreencherFormulario(Formulario() with { Orcamento = "Até R$ 5.000" });

        (briefing.Orcamento, briefing.Fecho).Should().Be(("Até R$ 5.000", "Gaveta"));
    }

    [Test]
    public void DeveNumerarAsMensagensNaOrdemDaConversa()
    {
        var briefing = Briefing.Criar(7, Formulario());

        briefing.RegistrarMensagem(PapelMensagem.Joalheiro, null, "{}", Agora);
        briefing.RegistrarMensagem(PapelMensagem.Assistente, " Qual o aro? ", "{}", Agora);

        briefing.Mensagens.Select(m => (m.Ordem, m.Papel, m.Texto)).Should().Equal(
            (1, PapelMensagem.Joalheiro, (string?)null),
            (2, PapelMensagem.Assistente, "Qual o aro?"));
    }

    [Test]
    public void DeveExigirConteudoNaMensagem()
    {
        var briefing = Briefing.Criar(7, Formulario());

        var registrar = () => briefing.RegistrarMensagem(PapelMensagem.Assistente, "Oi", " ", Agora);

        registrar.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveRecusarDocumentoVazio()
    {
        var criar = () => DocumentoBriefing.Criar(1, "briefing.pdf", [], Agora);

        criar.Should().Throw<ValorInvalidoException>();
    }

    private static FormularioBriefing Formulario(string? medidas = null)
        => new(TipoJoia.Anel, "Anel de noivado delicado", "Noivado", "Ouro amarelo", medidas, new DateOnly(2026, 12, 20), "Até R$ 4.000");
}
