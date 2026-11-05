using Castwork.Application.Abstractions;
using Castwork.Domain.Common;

using FluentValidation;

namespace Castwork.Application.Common.Validators;

public static class DefaultValidatorExtensions
{
    /// <summary>
    /// O Id precisa existir em <typeparamref name="TEntidade"/>. Se a entidade é <see cref="IAtivavel"/>,
    /// só conta registro ativo (não se cria peça para cliente desativado).
    /// </summary>
    public static IRuleBuilderOptions<T, long> MustExists<T, TEntidade>(this IRuleBuilder<T, long> rule, IUnitOfWork unitOfWork)
        where TEntidade : BaseEntity<long>, IAggregateRoot
        => rule
            .MustAsync((id, cancellationToken) => ExisteAsync<TEntidade>(unitOfWork, id, cancellationToken))
            .WithMessage("{PropertyName} não encontrado.");

    /// <summary>
    /// Variante para Id opcional: nulo é válido (use <c>NotNull</c> antes se for obrigatório).
    /// </summary>
    public static IRuleBuilderOptions<T, long?> MustExists<T, TEntidade>(this IRuleBuilder<T, long?> rule, IUnitOfWork unitOfWork)
        where TEntidade : BaseEntity<long>, IAggregateRoot
        => rule
            .MustAsync((id, cancellationToken) => id is null
                ? Task.FromResult(true)
                : ExisteAsync<TEntidade>(unitOfWork, id.Value, cancellationToken))
            .WithMessage("{PropertyName} não encontrado.");

    private static Task<bool> ExisteAsync<TEntidade>(IUnitOfWork unitOfWork, long id, CancellationToken cancellationToken)
        where TEntidade : BaseEntity<long>, IAggregateRoot
    {
        var query = unitOfWork.GetRepository<TEntidade>().FindBy(e => e.Id == id);

        if (typeof(IAtivavel).IsAssignableFrom(typeof(TEntidade)))
        {
            query = query.Where(e => ((IAtivavel)e).Ativo);
        }

        return query.AnyAsync(cancellationToken);
    }
}
