using Castwork.Domain.Entities.Briefings;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Briefings;

public class ImagemBriefingTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 6, 13, 0, 0, TimeSpan.Zero);

    [TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D }, "image/png")]
    [TestCase(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [TestCase(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50, 0x56 }, "image/webp")]
    public void DeveReconhecerOTipoPeloConteudo(byte[] conteudo, string tipo)
    {
        var imagem = ImagemBriefing.Criar(3, conteudo, Agora);

        imagem.Should().BeEquivalentTo(new { BriefingId = 3L, Tipo = tipo, EnviadaEm = Agora });
        imagem.Chave.Should().NotBeEmpty();
    }

    [Test]
    public void DeveRecusarArquivoQueNaoEImagem()
    {
        var criar = () => ImagemBriefing.Criar(3, "%PDF-1.7"u8.ToArray(), Agora);

        criar.Should().Throw<ValorInvalidoException>().WithMessage("*PNG, JPEG ou WebP*");
    }

    [Test]
    public void DeveRecusarImagemMaiorQueOLimite()
    {
        var conteudo = new byte[ImagemBriefing.TamanhoMaximo + 1];
        conteudo[0] = 0xFF;
        conteudo[1] = 0xD8;
        conteudo[2] = 0xFF;

        var criar = () => ImagemBriefing.Criar(3, conteudo, Agora);

        criar.Should().Throw<ValorInvalidoException>().WithMessage("*5 MB*");
    }

    [Test]
    public void DeveGuardarAsImagensNaMensagemDoJoalheiro()
    {
        var briefing = Briefing.Criar(7, new FormularioBriefing(TipoJoia.Anel, "Anel faixa", null, null, null, null, null));
        var chave = Guid.CreateVersion7();

        var mensagem = briefing.RegistrarMensagem(PapelMensagem.Joalheiro, null, "{}", Agora, [chave]);

        mensagem.Imagens.Should().Equal(chave);
        mensagem.Texto.Should().BeNull();
    }
}
