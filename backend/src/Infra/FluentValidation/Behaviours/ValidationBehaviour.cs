using FluentValidation;

using MediatR;

namespace Castwork.Infra.FluentValidation.Behaviours;

/// <summary>
/// 3º do pipeline: roda todos os validators do request e, havendo erro, lança <see cref="ValidationException"/>
/// com todos os erros juntos (a API devolve 400 com erros por campo). Os validators rodam em sequência,
/// e não em paralelo, porque compartilham o mesmo DbContext do escopo, que não aceita consultas simultâneas.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var erros = new List<global::FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var resultado = await validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken);
            erros.AddRange(resultado.Errors);
        }

        return erros.Count > 0 ? throw new ValidationException(erros) : await next(cancellationToken);
    }
}
