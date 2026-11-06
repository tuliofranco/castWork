using System.Reflection;

using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Atelie;

public class MarcaDoAtelieTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    [Test]
    public void DeveNormalizarAMarca()
    {
        var atelie = NovaConfiguracao();

        atelie.AtualizarMarca(" Joias autorais ", "#2e6e68", " @atelieaurora ", "atelieaurora.com", null);

        atelie.Should().BeEquivalentTo(new { Assinatura = "Joias autorais", CorMarca = "#2E6E68", Instagram = "atelieaurora", AvisoBriefing = (string?)null });
    }

    [Test]
    public void DeveVoltarACorPadraoQuandoVazia()
    {
        var atelie = NovaConfiguracao();

        atelie.AtualizarMarca(null, " ", null, null, null);

        atelie.CorMarca.Should().Be(ConfiguracaoAtelie.CorPadrao);
    }

    [TestCase("verde")]
    [TestCase("#12345")]
    [TestCase("2E6E68")]
    public void DeveRecusarCorForaDoFormato(string cor)
    {
        var atualizar = () => NovaConfiguracao().AtualizarMarca(null, cor, null, null, null);

        atualizar.Should().Throw<ValorInvalidoException>().Which.Campo.Should().Be(nameof(ConfiguracaoAtelie.CorMarca));
    }

    [Test]
    public void DeveAceitarLogoPngPeloConteudo()
    {
        var atelie = NovaConfiguracao();

        atelie.DefinirLogo(Png);

        (atelie.LogoTipo, atelie.Logo).Should().Be(("image/png", Png));
    }

    [Test]
    public void DeveRecusarArquivoQueNaoEImagem()
    {
        var definir = () => NovaConfiguracao().DefinirLogo("<svg onload=alert(1)>"u8.ToArray());

        definir.Should().Throw<ValorInvalidoException>().WithMessage("*PNG ou JPEG*");
    }

    [Test]
    public void DeveRecusarLogoMaiorQueOLimite()
    {
        var grande = new byte[ConfiguracaoAtelie.LogoTamanhoMaximo + 1];
        Png.CopyTo(grande, 0);

        var definir = () => NovaConfiguracao().DefinirLogo(grande);

        definir.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveRemoverOLogo()
    {
        var atelie = NovaConfiguracao();
        atelie.DefinirLogo(Png);

        atelie.RemoverLogo();

        (atelie.Logo, atelie.LogoTipo).Should().Be(((byte[]?)null, (string?)null));
    }

    private static ConfiguracaoAtelie NovaConfiguracao()
        => (ConfiguracaoAtelie)typeof(ConfiguracaoAtelie).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)!.Invoke(null);
}
