using Castwork.Application.Common.Exceptions;
using Castwork.Application.Projetos.Commands.CriarProjeto;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Infra;

/// <summary>
/// Autorização pelo pipeline real do MediatR, com um request de exemplo que exige [Permissao].
/// </summary>
public class AutorizacaoTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveLancarUnauthorizedQuandoNaoHaUsuarioLogadoAsync()
    {
        AmbienteDeTeste.Sessao.UsuarioId = null;

        var enviar = () => AmbienteDeTeste.SendAsync(new CriarProjetoCommand { Nome = "Colar" });

        await enviar.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task DeveLancarForbiddenQuandoUsuarioNaoAdminNaoTemARegraAsync()
    {
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = 2;

        var enviar = () => AmbienteDeTeste.SendAsync(new CriarProjetoCommand { Nome = "Colar" });

        await enviar.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task DeveAutorizarAntesDeValidarParaNaoRevelarSeOIdExisteAsync()
    {
        var command = await Usar(new ProjetoBuilder()).DoCliente(999_999).BuildCommandAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = 2;

        var enviar = () => AmbienteDeTeste.SendAsync(command);

        await enviar.Should().ThrowAsync<ForbiddenAccessException>("sem permissão, o usuário não pode descobrir pela validação se o cliente 999999 existe");
    }

    [Test]
    public async Task DevePermitirAdminExecutarQualquerRequestAsync()
    {
        var projetos = Usar(new ProjetoBuilder());

        var id = await projetos.SendAsync(await projetos.BuildCommandAsync());

        id.Should().BePositive();
    }
}
