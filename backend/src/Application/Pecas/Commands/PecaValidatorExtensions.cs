using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Pecas.Commands;

public static class PecaValidatorExtensions
{
    public const decimal PesoMaximoEmGramas = 100_000m;

    /// <summary>
    /// Folga larga para uma pedra cara: custo maior que isso por unidade é erro de digitação.
    /// </summary>
    public const decimal CustoMaximo = 10_000_000m;

    /// <summary>
    /// Nome obrigatório, tipo conhecido, teor em quilates ou milésimos, peso positivo e custo não negativo.
    /// </summary>
    public static void ValidarDadosDaPeca<T>(this AbstractValidator<T> validator)
        where T : DadosDaPeca
    {
        validator.RuleFor(c => c.Nome).NotEmpty().MaximumLength(Peca.NomeMaxLength).WithName("Nome");
        validator.RuleFor(c => c.Tipo).IsInEnum().WithName("Tipo");
        validator.RuleFor(c => c.Material).MaximumLength(Peca.MaterialMaxLength).WithName("Material");
        validator.RuleFor(c => c.Descricao).MaximumLength(Peca.DescricaoMaxLength).WithName("Descrição");

        validator.RuleFor(c => c.Teor!)
            .Must(SerTeorValido).WithMessage("Teor inválido. Use quilates (18k) ou milésimos (750).")
            .When(c => !string.IsNullOrWhiteSpace(c.Teor))
            .WithName("Teor");

        validator.RuleFor(c => c.PesoGramas)
            .GreaterThan(0)
            .LessThanOrEqualTo(PesoMaximoEmGramas)
            .PrecisionScale(10, 3, ignoreTrailingZeros: true)
            .When(c => c.PesoGramas is not null)
            .WithName("Peso");

        validator.RuleFor(c => c.Custo)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(CustoMaximo)
            .PrecisionScale(18, Dinheiro.CasasDecimais, ignoreTrailingZeros: true)
            .When(c => c.Custo is not null)
            .WithName("Custo");
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
