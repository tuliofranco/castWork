using Castwork.Application.Abstractions;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Castwork.Infra.MediatR.Behaviours;

/// <summary>
/// Último do pipeline (mede só o handler): avisa no log quando um request passa do limite.
/// Usa o <see cref="TimeProvider"/> para ser testável. Loga nome do request e usuário, nunca o conteúdo.
/// </summary>
public sealed partial class PerformanceBehaviour<TRequest, TResponse>(
    ILogger<PerformanceBehaviour<TRequest, TResponse>> logger,
    ISessaoService sessao,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly TimeSpan Limite = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var inicio = timeProvider.GetTimestamp();
        var resposta = await next(cancellationToken);
        var duracao = timeProvider.GetElapsedTime(inicio);

        if (duracao > Limite)
        {
            LogLento(logger, typeof(TRequest).Name, (long)duracao.TotalMilliseconds, sessao.UsuarioId);
        }

        return resposta;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request lento: {Request} levou {DuracaoMs} ms (usuário {UsuarioId})")]
    private static partial void LogLento(ILogger logger, string request, long duracaoMs, long? usuarioId);
}
