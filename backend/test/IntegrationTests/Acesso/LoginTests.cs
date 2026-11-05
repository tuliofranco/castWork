using System.Security.Cryptography;
using System.Text;

using Castwork.Application.Acesso.Commands.Login;
using Castwork.Domain.Entities.Acesso;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Acesso;

public class LoginTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveEntrarERetornarTokensGuardandoSoOHashDoRefreshAsync()
    {
        var usuarios = Usar(new UsuarioBuilder());
        var command = await usuarios.CustomizeCommand(c => (c.Nome, c.Email) = ("Ana Lima", "ana@atelie.com")).BuildCommandAsync();
        var usuarioId = await usuarios.SendAsync(command);

        var sessao = await AmbienteDeTeste.SendAsync(new LoginCommand { Email = "  ANA@atelie.com ", Senha = UsuarioBuilder.SenhaPadrao });

        sessao.Should().BeEquivalentTo(new
        {
            UsuarioId = usuarioId,
            Nome = "Ana Lima",
            IsAdmin = false,
            AccessTokenExpiraEm = RelogioDeTeste.Inicio.AddMinutes(15),
            RefreshTokenExpiraEm = RelogioDeTeste.Inicio.AddDays(7),
        });
        var hashEsperado = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sessao.RefreshToken)));
        var gravados = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.RefreshTokens.Select(t => t.TokenHash).ToListAsync());
        gravados.Should().Equal(hashEsperado);
        var usuario = await LerUsuarioAsync(usuarioId);
        usuario.UltimoLoginEm.Should().Be(RelogioDeTeste.Inicio);
    }

    [Test]
    public async Task DeveRejeitarSenhaErradaEEmailInexistenteComAMesmaMensagemAsync()
    {
        var command = await Usar(new UsuarioBuilder()).CustomizeCommand(c => c.Email = "ana@atelie.com").BuildCommandAsync();
        await Usar(new UsuarioBuilder()).SendAsync(command);

        var senhaErrada = () => AmbienteDeTeste.SendAsync(new LoginCommand { Email = "ana@atelie.com", Senha = "senha-errada-1" });
        var emailInexistente = () => AmbienteDeTeste.SendAsync(new LoginCommand { Email = "nao@existe.com", Senha = "senha-errada-1" });

        (await senhaErrada.Should().ThrowAsync<UnauthorizedAccessException>()).Which.Message.Should().Be(LoginCommandHandler.MensagemFalha);
        (await emailInexistente.Should().ThrowAsync<UnauthorizedAccessException>()).Which.Message.Should().Be(LoginCommandHandler.MensagemFalha);
    }

    [Test]
    public async Task DeveBloquearAposCincoFalhasMesmoComSenhaCorretaELiberarDepoisDeQuinzeMinutosAsync()
    {
        var usuarios = Usar(new UsuarioBuilder());
        var command = await usuarios.CustomizeCommand(c => c.Email = "ana@atelie.com").BuildCommandAsync();
        await usuarios.SendAsync(command);
        for (var i = 0; i < Usuario.MaximoDeTentativas; i++)
        {
            await FluentActions.Awaiting(() => Entrar("senha-errada-1")).Should().ThrowAsync<UnauthorizedAccessException>();
        }

        var comSenhaCorretaBloqueado = () => Entrar(UsuarioBuilder.SenhaPadrao);

        await comSenhaCorretaBloqueado.Should().ThrowAsync<UnauthorizedAccessException>();
        AmbienteDeTeste.Relogio.Avancar(Usuario.DuracaoDoBloqueio);
        (await Entrar(UsuarioBuilder.SenhaPadrao)).AccessToken.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task DeveRejeitarUsuarioDesativadoAsync()
    {
        var usuarios = Usar(new UsuarioBuilder());
        var command = await usuarios.CustomizeCommand(c => c.Email = "ana@atelie.com").BuildCommandAsync();
        var id = await usuarios.SendAsync(command);
        await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Usuarios.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Ativo, false)));

        var entrar = () => Entrar(UsuarioBuilder.SenhaPadrao);

        await entrar.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static Task<Castwork.Application.Acesso.Common.SessaoDto> Entrar(string senha)
        => AmbienteDeTeste.SendAsync(new LoginCommand { Email = "ana@atelie.com", Senha = senha });

    private static Task<Usuario> LerUsuarioAsync(long id)
        => AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == id));
}
