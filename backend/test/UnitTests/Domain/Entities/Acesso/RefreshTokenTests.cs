using Castwork.Domain.Entities.Acesso;
using Castwork.Domain.Exceptions;

namespace Castwork.UnitTests.Domain.Entities.Acesso;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 13, 0, 0, TimeSpan.Zero);
    private static readonly string Hash = new('a', RefreshToken.TokenHashLength);

    [Test]
    public void DeveEmitirTokenAtivoAteExpirar()
    {
        var token = Emitir();

        token.Should().BeEquivalentTo(new { UsuarioId = 1L, ExpiraEm = Agora.AddDays(7), UsadoEm = (DateTimeOffset?)null });
        token.EstaAtivo(Agora.AddDays(7).AddSeconds(-1)).Should().BeTrue();
        token.EstaAtivo(Agora.AddDays(7)).Should().BeFalse();
    }

    [Test]
    public void DeveRejeitarHashComTamanhoErrado()
    {
        var emitir = () => RefreshToken.Emitir(1, "curto", Guid.NewGuid(), Guid.NewGuid(), Agora, TimeSpan.FromDays(7));

        emitir.Should().Throw<ValorInvalidoException>();
    }

    [Test]
    public void DeveImpedirUsoDuplicadoDoMesmoToken()
    {
        var token = Emitir();
        token.MarcarComoUsado(Agora);

        var usarDeNovo = () => token.MarcarComoUsado(Agora);

        usarDeNovo.Should().Throw<RegraDeNegocioException>();
        token.FoiUsadoOuRevogado.Should().BeTrue();
    }

    [Test]
    public void DeveManterPrimeiraRevogacaoAoRevogarDeNovo()
    {
        var token = Emitir();
        token.Revogar(Agora, "Logout");

        token.Revogar(Agora.AddHours(1), "Outro motivo");

        token.Should().BeEquivalentTo(new { RevogadoEm = (DateTimeOffset?)Agora, MotivoRevogacao = "Logout" });
    }

    private static RefreshToken Emitir() => RefreshToken.Emitir(1, Hash, Guid.NewGuid(), Guid.NewGuid(), Agora, TimeSpan.FromDays(7));
}
