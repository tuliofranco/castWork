using Castwork.Infra.MediatR.Behaviours;

using Microsoft.Extensions.Logging;

using Moq;

namespace Castwork.UnitTests.Infra.MediatR;

public class UnhandledExceptionBehaviourTests
{
    [Test]
    public async Task DeveRegistrarErroComNomeDoRequestERelancarExcecaoAsync()
    {
        var logger = new Mock<ILogger<UnhandledExceptionBehaviour<RequestDeTeste, string>>>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var erro = new InvalidOperationException("falhou");
        var behaviour = new UnhandledExceptionBehaviour<RequestDeTeste, string>(logger.Object);

        var executar = () => behaviour.Handle(new RequestDeTeste("123.456.789-00"), _ => throw erro, CancellationToken.None);

        (await executar.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(erro);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((estado, _) => $"{estado}".Contains(nameof(RequestDeTeste)) && !$"{estado}".Contains("123.456")),
                erro,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task DeveIgnorarCancelamentoSemRegistrarErroAsync()
    {
        var logger = new Mock<ILogger<UnhandledExceptionBehaviour<RequestDeTeste, string>>>();
        var behaviour = new UnhandledExceptionBehaviour<RequestDeTeste, string>(logger.Object);

        var executar = () => behaviour.Handle(new RequestDeTeste("x"), _ => throw new OperationCanceledException(), CancellationToken.None);

        await executar.Should().ThrowAsync<OperationCanceledException>();
        logger.VerifyNoOtherCalls();
    }

    public sealed record RequestDeTeste(string Documento);
}
