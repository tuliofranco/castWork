using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

public sealed class CriarPecaCommandValidator : ValidatorBase<CriarPecaCommand>
{
    public const decimal PesoMaximoEmGramas = 100_000m;

    public CriarPecaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(Peca.NomeMaxLength).WithName("Nome");
        RuleFor(c => c.Tipo).IsInEnum().WithName("Tipo");
        RuleFor(c => c.Material).MaximumLength(Peca.MaterialMaxLength).WithName("Material");
        RuleFor(c => c.Descricao).MaximumLength(Peca.DescricaoMaxLength).WithName("Descrição");

        RuleFor(c => c.Teor!)
            .Must(SerTeorValido).WithMessage("Teor inválido. Use quilates (18k) ou milésimos (750).")
            .When(c => !string.IsNullOrWhiteSpace(c.Teor))
            .WithName("Teor");

        RuleFor(c => c.PesoGramas)
            .GreaterThan(0)
            .LessThanOrEqualTo(PesoMaximoEmGramas)
            .PrecisionScale(10, 3, ignoreTrailingZeros: true)
            .When(c => c.PesoGramas is not null)
            .WithName("Peso");
    }

    private static bool SerTeorValido(string teor)
    {
        try
        {
            TeorMetal.Parse(teor);
            return true;
        }
        catch (ValorInvalidoException)
        {
            return false;
        }
    }
}
