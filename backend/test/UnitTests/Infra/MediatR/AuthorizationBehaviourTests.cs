using Castwork.Application.Abstractions;
using Castwork.Application.Common.Exceptions;
using Castwork.Application.Common.Security;
using Castwork.Infra.MediatR.Behaviours;

using MediatR;

using Moq;

namespace Castwork.UnitTests.Infra.MediatR;

public class AuthorizationBehaviourTests
{
    private static readonly Guid RegraConsultarPecas = Guid.Parse(Regras.Pecas.Consultar);

    private Mock<ISessaoService> _sessao = null!;
    private Mock<IPermissaoService> _permissoes = null!;

    [SetUp]
    public void SetUp()
    {
        _sessao = new Mock<ISessaoService>();
        _permissoes = new Mock<IPermissaoService>(MockBehavior.Strict);
    }

    [Test]
    public async Task DevePassarRequestSemPermissaoMesmoSemUsuarioAsync()
    {
        var resposta = await Executar(new RequestPublico());

        resposta.Should().Be("ok");
    }

    [Test]
    public async Task DeveLancarUnauthorizedQuandoRequestExigeRegraESemUsuarioAsync()
    {
        var executar = () => Executar(new RequestComRegra());

        await executar.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task DeveLancarUnauthorizedQuandoRequestSomenteAutenticadoESemUsuarioAsync()
    {
        var executar = () => Executar(new RequestSomenteAutenticado());

        await executar.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task DevePassarRequestSomenteAutenticadoComUsuarioSemConsultarRegrasAsync()
    {
        Logar(admin: false, perfilId: 3);

        var resposta = await Executar(new RequestSomenteAutenticado());

        resposta.Should().Be("ok");
    }

    [Test]
    public async Task DevePassarAdminSemConsultarRegrasAsync()
    {
        Logar(admin: true, perfilId: null);

        var resposta = await Executar(new RequestComRegra());

        resposta.Should().Be("ok");
    }

    [Test]
    public async Task DevePassarUsuarioCujoPerfilPossuiARegraAsync()
    {
        Logar(admin: false, perfilId: 3);
        _permissoes.Setup(p => p.PerfilPossuiRegraAsync(3, RegraConsultarPecas, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resposta = await Executar(new RequestComRegra());

        resposta.Should().Be("ok");
    }

    [Test]
    public async Task DeveLancarForbiddenQuandoPerfilNaoPossuiARegraAsync()
    {
        Logar(admin: false, perfilId: 3);
        _permissoes.Setup(p => p.PerfilPossuiRegraAsync(3, RegraConsultarPecas, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var executar = () => Executar(new RequestComRegra());

        await executar.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task DeveLancarForbiddenQuandoUsuarioNaoAdminNaoTemPerfilAsync()
    {
        Logar(admin: false, perfilId: null);

        var executar = () => Executar(new RequestComRegra());

        await executar.Should().ThrowAsync<ForbiddenAccessException>();
    }

    private void Logar(bool admin, long? perfilId)
    {
        _sessao.Setup(s => s.UsuarioId).Returns(10);
        _sessao.Setup(s => s.IsAdmin).Returns(admin);
        _sessao.Setup(s => s.PerfilId).Returns(perfilId);
    }

    private Task<string> Executar<TRequest>(TRequest request)
        where TRequest : notnull
        => new AuthorizationBehaviour<TRequest, string>(_sessao.Object, _permissoes.Object)
            .Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

    [SemPermissao]
    private sealed class RequestPublico : IRequest<string>;

    [SomenteAutenticado]
    private sealed class RequestSomenteAutenticado : IRequest<string>;

    [Permissao(Regras.Pecas.Consultar)]
    private sealed class RequestComRegra : IRequest<string>;
}
