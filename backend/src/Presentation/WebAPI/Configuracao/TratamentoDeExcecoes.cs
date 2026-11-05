using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Exceptions;

using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Castwork.WebAPI.Configuracao;

/// <summary>
/// Traduz exceções em ProblemDetails (RFC 9457). Mensagens de exceções previstas são seguras para o usuário;
/// qualquer outra vira 500 genérico, sem stack trace nem mensagem interna (só o traceId para o suporte).
/// </summary>
public sealed class TratamentoDeExcecoes(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, titulo, detalhe) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Dados inválidos.", null),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, "Não autenticado.", ex.Message),
            ForbiddenAccessException ex => (StatusCodes.Status403Forbidden, "Acesso negado.", ex.Message),
            EntidadeNaoEncontradaException ex => (StatusCodes.Status404NotFound, "Não encontrado.", ex.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito de edição.", "O registro foi alterado por outra pessoa. Recarregue e tente de novo."),

            // Duas gravações simultâneas disputando a mesma chave única (ex.: dois envios na mesma conversa).
            DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Conflito de edição.", "Outra alteração foi salva ao mesmo tempo. Recarregue e tente de novo."),
            ServicoIndisponivelException ex => (StatusCodes.Status503ServiceUnavailable, "Serviço indisponível.", ex.Message),
            DomainException ex => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada.", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado.", "Ocorreu um erro inesperado. Informe o código de rastreio ao suporte."),
        };

        var problema = exception is ValidationException validacao
            ? new ValidationProblemDetails(validacao.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();

        problema.Status = status;
        problema.Title = titulo;
        problema.Detail = detalhe;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception,
        });
    }
}
