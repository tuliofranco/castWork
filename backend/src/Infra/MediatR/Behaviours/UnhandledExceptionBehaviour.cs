using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Exceptions;

using FluentValidation;

using MediatR;

using Microsoft.Extensions.Logging;

namespace Castwork.Infra.MediatR.Behaviours;

/// <summary>
/// 1º do pipeline: registra qualquer exceção que escape dos behaviours seguintes ou do handler, com o nome
/// do request, e relança (quem traduz para HTTP/GraphQL é a borda). Não loga o conteúdo do request:
/// ele pode ter dado pessoal (CPF, e-mail) ou senha.
/// </summary>
public sealed partial class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (!EhEsperada(ex))
        {
            LogExcecao(logger, ex, typeof(TRequest).Name);
            throw;
        }
    }

    /// <summary>
    /// Respostas previstas do sistema (400, 401, 403, 404, 422) e cancelamento não são erro de servidor.
    /// </summary>
    private static bool EhEsperada(Exception ex)
        => ex is OperationCanceledException
            or ValidationException
            or UnauthorizedAccessException
            or ForbiddenAccessException
            or DomainException;

    [LoggerMessage(Level = LogLevel.Error, Message = "Exceção não tratada no request {Request}")]
    private static partial void LogExcecao(ILogger logger, Exception exception, string request);
}
