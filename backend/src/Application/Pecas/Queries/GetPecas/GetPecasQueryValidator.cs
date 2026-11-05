using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

using FluentValidation;

namespace Castwork.Application.Pecas.Queries.GetPecas;

public sealed class GetPecasQueryValidator : ValidatorPaginado<GetPecasQuery, GetPecasDto>
{
    public const int TermoMaxLength = 100;

    public GetPecasQueryValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(q => q.Termo).MaximumLength(TermoMaxLength).WithName("Termo de busca");
        RuleFor(q => q.Status).IsInEnum();
        RuleFor(q => q.ClienteId).GreaterThan(0).When(q => q.ClienteId is not null).WithName("Cliente");
    }
}
