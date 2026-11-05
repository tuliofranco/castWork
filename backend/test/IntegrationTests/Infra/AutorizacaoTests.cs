using Castwork.Application.Common.Exceptions;
using Castwork.Application.Pecas.Commands.CriarPeca;
using Castwork.Application.Pecas.Queries.GetPecas;
using Castwork.TestBootstrap.Ambiente;
using Castwork.TestBootstrap.Builders;

namespace Castwork.IntegrationTests.Infra;

/// <summary>
/// Autorização pelo pipeline real do MediatR. Até a Etapa 6, perfis não têm regras (negar por padrão),
/// então só admin passa pelos requests com [Permissao].
/// </summary>
public class AutorizacaoTests : TesteDeIntegracao
{
    [Test]
    public async Task DeveLancarUnauthorizedQuandoNaoHaUsuarioLogadoAsync()
    {
        AmbienteDeTeste.Sessao.UsuarioId = null;

        var enviar = () => AmbienteDeTeste.SendAsync(new GetPecasQuery());

        await enviar.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task DeveLancarForbiddenQuandoUsuarioNaoAdminNaoTemARegraAsync()
    {
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = 2;

        var enviar = () => AmbienteDeTeste.SendAsync(new GetPecasQuery());

        await enviar.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task DeveAutorizarAntesDeValidarParaNaoRevelarSeOIdExisteAsync()
    {
        var command = await Usar(new PecaBuilder()).DoCliente(999_999).BuildCommandAsync();
        AmbienteDeTeste.Sessao.IsAdmin = false;
        AmbienteDeTeste.Sessao.PerfilId = 2;

        var enviar = () => AmbienteDeTeste.SendAsync<long>(command);

        await enviar.Should().ThrowAsync<ForbiddenAccessException>("sem permissão, o usuário não pode descobrir pela validação se o cliente 999999 existe");
    }

    [Test]
    public async Task DevePermitirAdminExecutarQualquerRequestAsync()
    {
        await Usar(new PecaBuilder()).BuildAsync();

        var resultado = await AmbienteDeTeste.SendAsync(new GetPecasQuery());

        resultado.Lista.Should().ContainSingle();
    }
}
