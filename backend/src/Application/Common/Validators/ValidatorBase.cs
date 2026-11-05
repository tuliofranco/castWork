using Castwork.Application.Abstractions;

using FluentValidation;

namespace Castwork.Application.Common.Validators;

/// <summary>
/// Base dos validators que consultam o banco (existência, unicidade).
/// </summary>
public abstract class ValidatorBase<T>(IUnitOfWork unitOfWork) : AbstractValidator<T>
{
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
}
