using Castwork.Application.Common.Exceptions;
using Castwork.Domain.Exceptions;

using FluentValidation;

namespace Castwork.GraphQl.Erros;

/// <summary>
/// Mesmo contrato de erros do REST, em formato GraphQL: código estável em <c>extensions.code</c>,
/// mensagem segura para o usuário e erros de validação por campo. Exceção inesperada vira mensagem genérica,
/// sem detalhes internos (o detalhe fica no log).
/// </summary>
public sealed class ErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
        => error.Exception switch
        {
            ValidationException validacao => Traduzir(error, "VALIDACAO", "Dados inválidos.")
                .SetExtension("erros", validacao.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            UnauthorizedAccessException ex => Traduzir(error, "NAO_AUTENTICADO", ex.Message),
            ForbiddenAccessException ex => Traduzir(error, "ACESSO_NEGADO", ex.Message),
            EntidadeNaoEncontradaException ex => Traduzir(error, "NAO_ENCONTRADO", ex.Message),
            DomainException ex => Traduzir(error, "REGRA_DE_NEGOCIO", ex.Message),
            null => error,
            _ => Traduzir(error, "ERRO_INTERNO", "Ocorreu um erro inesperado."),
        };

    private static IError Traduzir(IError error, string codigo, string mensagem)
        => error.WithMessage(mensagem).WithCode(codigo).WithException(null);
}
