using Castwork.Application.Acesso.Commands.AlterarMinhaSenha;
using Castwork.Application.Acesso.Commands.Login;
using Castwork.Application.Acesso.Commands.Logout;
using Castwork.Application.Acesso.Commands.RenovarSessao;
using Castwork.Application.Acesso.Common;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Acesso;

public class RenovarSessaoTests : TesteDeIntegracao
{
    private long _usuarioId;

    [SetUp]
    public async Task CriarUsuarioAsync()
    {
        var usuarios = Usar(new UsuarioBuilder());
        _usuarioId = await usuarios.SendAsync(await usuarios.CustomizeCommand(c => c.Email = "ana@atelie.com").BuildCommandAsync());
    }

    [Test]
    public async Task DeveTrocarRefreshTokenPorNovoParNaMesmaFamiliaAsync()
    {
        var login = await EntrarAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromMinutes(20));

        var renovada = await Renovar(login.RefreshToken);

        renovada.RefreshToken.Should().NotBe(login.RefreshToken);
        renovada.AccessTokenExpiraEm.Should().Be(RelogioDeTeste.Inicio.AddMinutes(35));
        var tokens = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.RefreshTokens.OrderBy(t => t.Id).AsNoTracking().ToListAsync());
        tokens.Select(t => t.Familia).Distinct().Should().ContainSingle();
        tokens.Select(t => t.UsadoEm).Should().Equal(RelogioDeTeste.Inicio.AddMinutes(20), null);
    }

    [Test]
    public async Task DeveRevogarFamiliaInteiraQuandoTokenJaUsadoEReutilizadoAsync()
    {
        var login = await EntrarAsync();
        var renovada = await Renovar(login.RefreshToken);

        var reuso = () => Renovar(login.RefreshToken);

        await reuso.Should().ThrowAsync<UnauthorizedAccessException>();
        await FluentActions.Awaiting(() => Renovar(renovada.RefreshToken)).Should().ThrowAsync<UnauthorizedAccessException>("quem roubou o token antigo não pode manter a sessão");
    }

    [Test]
    public async Task DeveRejeitarRefreshTokenExpiradoAsync()
    {
        var login = await EntrarAsync();
        AmbienteDeTeste.Relogio.Avancar(TimeSpan.FromDays(7));

        var renovar = () => Renovar(login.RefreshToken);

        (await renovar.Should().ThrowAsync<UnauthorizedAccessException>()).Which.Message.Should().Be(EmissorDeSessao.MensagemSessaoInvalida);
    }

    [Test]
    public async Task DeveDerrubarOutrasSessoesAoAlterarSenhaMantendoADesteDispositivoAsync()
    {
        var outroDispositivo = await EntrarAsync();
        AmbienteDeTeste.Sessao.UsuarioId = _usuarioId;
        AmbienteDeTeste.Sessao.IsAdmin = false;

        var novaSessao = await AmbienteDeTeste.SendAsync(new AlterarMinhaSenhaCommand { SenhaAtual = UsuarioBuilder.SenhaPadrao, NovaSenha = "nova-senha-segura" });

        await FluentActions.Awaiting(() => Renovar(outroDispositivo.RefreshToken)).Should().ThrowAsync<UnauthorizedAccessException>();
        (await Renovar(novaSessao.RefreshToken)).UsuarioId.Should().Be(_usuarioId);
        (await AmbienteDeTeste.SendAsync(new LoginCommand { Email = "ana@atelie.com", Senha = "nova-senha-segura" })).Should().NotBeNull();
    }

    [Test]
    public async Task DeveRejeitarTrocaDeSenhaComSenhaAtualErradaAsync()
    {
        AmbienteDeTeste.Sessao.UsuarioId = _usuarioId;

        var alterar = () => AmbienteDeTeste.SendAsync(new AlterarMinhaSenhaCommand { SenhaAtual = "nao-e-esta", NovaSenha = "nova-senha-segura" });

        (await alterar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PropertyName = "senhaAtual", ErrorMessage = "Senha atual incorreta." });
    }

    [Test]
    public async Task DeveEncerrarSessaoNoLogoutAsync()
    {
        var login = await EntrarAsync();

        await AmbienteDeTeste.SendAsync(new LogoutCommand { RefreshToken = login.RefreshToken });

        await FluentActions.Awaiting(() => Renovar(login.RefreshToken)).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static Task<SessaoDto> EntrarAsync()
        => AmbienteDeTeste.SendAsync(new LoginCommand { Email = "ana@atelie.com", Senha = UsuarioBuilder.SenhaPadrao });

    private static Task<SessaoDto> Renovar(string refreshToken)
        => AmbienteDeTeste.SendAsync(new RenovarSessaoCommand { RefreshToken = refreshToken });
}
