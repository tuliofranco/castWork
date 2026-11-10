using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Precificacao.Commands;

public static class LigaValidatorExtensions
{
    /// <summary>
    /// Nome único entre as ligas ativas, metal conhecido, teor em quilates ou milésimos e densidade positiva.
    /// <paramref name="idAtual"/> é a liga sendo alterada (ela mesma não conta como repetida).
    /// </summary>
    public static void ValidarDadosDaLiga<T>(this AbstractValidator<T> validator, IUnitOfWork unitOfWork, Func<T, long> idAtual)
        where T : DadosDaLiga
    {
        validator.RuleFor(c => c.Nome)
            .NotEmpty()
            .MaximumLength(Liga.NomeMaxLength)
            .MustAsync((command, nome, cancellationToken) => NomeLivreAsync(unitOfWork, nome, idAtual(command), cancellationToken))
            .WithMessage("Já existe uma liga ativa com este nome.")
            .WithName("Nome");

        validator.RuleFor(c => c.Metal).IsInEnum().WithName("Metal");

        validator.RuleFor(c => c.Teor)
            .NotEmpty()
            .Must(SerTeorValido).WithMessage("Teor inválido. Use quilates (18k) ou milésimos (750).")
            .WithName("Teor");

        validator.RuleFor(c => c.Densidade)
            .GreaterThan(0)
            .LessThanOrEqualTo(Liga.DensidadeMaxima)
            .PrecisionScale(6, Liga.DensidadeCasasDecimais, ignoreTrailingZeros: true)
            .WithName("Densidade");
    }

    private static async Task<bool> NomeLivreAsync(IUnitOfWork unitOfWork, string nome, long idAtual, CancellationToken cancellationToken)
    {
#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var normalizado = nome.Trim().ToLower();
        var repetido = await unitOfWork.GetRepository<Liga>()
            .FindBy(l => l.Ativo && l.Id != idAtual && l.Nome.ToLower() == normalizado)
            .AnyAsync(cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862
        return !repetido;
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
