using Castwork.Application.Abstractions;
using Castwork.Infra.MediatR.Behaviours;

using Microsoft.Extensions.Logging;

using Moq;

namespace Castwork.UnitTests.Infra.MediatR;

public class PerformanceBehaviourTests
{
    [TestCase(501, 1)]
    [TestCase(500, 0)]
    public async Task DeveAvisarNoLogSoQuandoRequestPassaDoLimiteAsync(int duracaoMs, int avisosEsperados)
    {
        var logger = new Mock<ILogger<PerformanceBehaviour<string, string>>>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var relogio = new Mock<TimeProvider> { CallBase = true };
        relogio.SetupSequence(r => r.GetTimestamp())
            .Returns(0)
            .Returns(duracaoMs * TimeProvider.System.TimestampFrequency / 1000);
        var behaviour = new PerformanceBehaviour<string, string>(logger.Object, Mock.Of<ISessaoService>(), relogio.Object);

        var resposta = await behaviour.Handle("request", _ => Task.FromResult("ok"), CancellationToken.None);

        resposta.Should().Be("ok");
        logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(avisosEsperados));
    }
}
