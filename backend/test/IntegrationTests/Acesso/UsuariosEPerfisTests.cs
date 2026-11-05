using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Application.Projetos.Commands.CriarProjeto;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace Castwork.IntegrationTests.Acesso;

public class UsuariosEPerfisTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveTerSincronizadoTodoOCatalogoDeRegrasNaInicializacaoAsync()
    {
        var noBanco = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Regras.AsNoTracking().ToListAsync());

        noBanco.Select(r => new { r.Id, r.Codigo, r.Grupo, r.Descricao }).Should()
            .BeEquivalentTo(CatalogoDeRegras.Todas.Select(r => new { r.Id, r.Codigo, r.Grupo, r.Descricao }));
    }

    [Test]
    public async Task DevePermitirUsuarioCujoPerfilTemARegraEBarrarQuemNaoTemAsync()
    {
        var comRegra = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Criar).BuildAsync();
        var semRegra = await Usar(new PerfilBuilder()).ComRegras(Regras.Projetos.Consultar).BuildAsync();
        var projetos = Usar(new ProjetoBuilder());
        AmbienteDeTeste.Sessao.IsAdmin = false;

        AmbienteDeTeste.Sessao.PerfilId = comRegra;
        var permitido = await projetos.SendAsync(await projetos.BuildCommandAsync());
        AmbienteDeTeste.Sessao.PerfilId = semRegra;
        var negado = () => AmbienteDeTeste.SendAsync(new CriarProjetoCommand { Nome = "Colar" });

        permitido.Should().BePositive();
        await negado.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task DeveRejeitarPerfilComRegraForaDoCatalogoAsync()
    {
        var perfis = Usar(new PerfilBuilder());
        var command = await perfis.CustomizeCommand(c => c.Regras = [Guid.Empty]).BuildCommandAsync();

        var enviar = () => perfis.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be($"Regra '{Guid.Empty}' não existe.");
    }

    [Test]
    public async Task DeveRejeitarEmailRepetidoSemDiferenciarMaiusculasESenhaCurtaAsync()
    {
        await Usar(new UsuarioBuilder()).CustomizeCommand(c => c.Email = "ana@atelie.com").BuildAsync();
        var usuarios = Usar(new UsuarioBuilder());
        var command = await usuarios.CustomizeCommand(c => (c.Email, c.Senha) = ("ANA@Atelie.com", "curta")).BuildCommandAsync();

        var enviar = () => usuarios.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Select(e => (e.PropertyName, e.ErrorMessage)).Should().BeEquivalentTo(new[]
        {
            ("email", "Já existe um usuário com este e-mail."),
            ("senha", "'Senha' deve ser maior ou igual a 8 caracteres. Você digitou 5 caracteres."),
        });
    }

    [Test]
    public async Task DeveImpedirQueNaoAdminCrieAdministradorAsync()
    {
        var gestor = await Usar(new PerfilBuilder()).ComRegras(Regras.Acesso.GerenciarUsuarios).BuildAsync();
        var usuarios = Usar(new UsuarioBuilder()).Admin();
        var command = await usuarios.BuildCommandAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = gestor;

        var enviar = () => usuarios.SendAsync(command);

        (await enviar.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Somente administradores podem criar outro administrador.");
    }

    [Test]
    public async Task DeveGravarSenhaDoUsuarioSomenteComoHashAsync()
    {
        var id = await Usar(new UsuarioBuilder()).BuildAsync();

        var usuario = await AmbienteDeTeste.ExecutarNoBancoAsync(db => db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == id));

        // Formato v3 do PasswordHasher: 61 bytes (versão, PRF, iterações, salt de 16, subchave de 32) em base64.
        usuario.SenhaHash.Should().NotContain(UsuarioBuilder.SenhaPadrao).And.HaveLength(84);
    }
}
