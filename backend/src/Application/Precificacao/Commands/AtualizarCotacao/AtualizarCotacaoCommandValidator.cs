using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;

using FluentValidation;

namespace Castwork.Application.Precificacao.Commands.AtualizarCotacao;

public sealed class AtualizarCotacaoCommandValidator : ValidatorBase<AtualizarCotacaoCommand>
{
    /// <summary>
    /// Folga larga sobre o ouro puro (na casa de R$ 600/g em 2026): valor maior é erro de digitação.
    /// </summary>
    public const decimal ValorMaximo = 100_000m;

    public AtualizarCotacaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Metal).IsInEnum().WithName("Metal");
        RuleFor(c => c.Valor)
            .GreaterThan(0)
            .LessThanOrEqualTo(ValorMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Cotação");
    }
}
