using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Assistente;

public class ImagemConversaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 6, 13, 0, 0, TimeSpan.Zero);

    [TestCase(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D }, "image/png")]
    [TestCase(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "image/jpeg")]
    [TestCase(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50, 0x56 }, "image/webp")]
    public void DeveReconhecerOTipoPeloConteudo(byte[] conteudo, string tipo)
    {
        var imagem = ImagemConversa.Criar(3, conteudo, Agora);

        imagem.Should().BeEquivalentTo(new { ConversaId = 3L, Tipo = tipo, CriadaEm = Agora });
        imagem.Chave.Should().NotBeEmpty();
    }

    [Test]
    public void DeveRecusarArquivoQueNaoEImagem()
    {
        var criar = () => ImagemConversa.Criar(3, "%PDF-1.7"u8.ToArray(), Agora);

        criar.Should().Throw<ValorInvalidoException>().WithMessage("*PNG, JPEG ou WebP*");
    }
}
