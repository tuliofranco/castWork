using System.Text;

using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Assistente;

/// <summary>
/// As imagens guardadas do arquivo e do desenho da peça da biblioteca: só PNG ou SVG, numa vista conhecida, e uma por
/// vista e tipo no desenho.
/// </summary>
public class ImagemDoArquivoTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1];
    private static readonly byte[] Svg = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\"/>");

    [Test]
    public void DeveDetectarOTipoPeloConteudo()
    {
        ImagemDoArquivo.Criar(1, VistaDaImagem.Iso, Png, DateTimeOffset.UnixEpoch).Tipo.Should().Be(TipoDaImagemGuardada.Png);
        ImagemDoArquivo.Criar(1, VistaDaImagem.Desenho2d, Svg, DateTimeOffset.UnixEpoch).Tipo.Should().Be(TipoDaImagemGuardada.Svg);
    }

    [Test]
    public void DeveRecusarVistaDesconhecidaConteudoVazioOuQueNaoEImagem()
    {
        var vista = () => ImagemDoArquivo.Criar(1, "costas", Png, DateTimeOffset.UnixEpoch);
        var vazia = () => ImagemDoArquivo.Criar(1, VistaDaImagem.Iso, [], DateTimeOffset.UnixEpoch);
        var texto = () => ImagemDoArquivo.Criar(1, VistaDaImagem.Iso, "DXF"u8.ToArray(), DateTimeOffset.UnixEpoch);

        vista.Should().Throw<ValorInvalidoException>();
        vazia.Should().Throw<ValorInvalidoException>();
        texto.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveGuardarAsImagensDoDesenhoDaPecaUmaPorVistaETipo()
    {
        var peca = Peca.Criar("Elo", TipoPeca.Elo, liga: null, materialLivre: null, peso: null, descricao: null);

        peca.GuardarDesenho("desenho-2d", [1], DateTimeOffset.UnixEpoch, [(VistaDaImagem.Desenho2d, Svg), (VistaDaImagem.Desenho2d, Png), (VistaDaImagem.Desenho2d, Png)]);

        peca.Desenhos.Single().Imagens.Select(i => (i.Vista, i.Tipo)).Should().Equal(
            (VistaDaImagem.Desenho2d, TipoDaImagemGuardada.Svg), (VistaDaImagem.Desenho2d, TipoDaImagemGuardada.Png));
    }
}
