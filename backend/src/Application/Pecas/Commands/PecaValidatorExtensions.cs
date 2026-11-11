using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Pecas.Commands;

public static class PecaValidatorExtensions
{
    public const decimal PesoMaximoEmGramas = 100_000m;

    /// <summary>
    /// Folga larga para uma pedra cara: valor maior que isso por unidade é erro de digitação.
    /// </summary>
    public const decimal ValorMaximo = 10_000_000m;

    /// <summary>
    /// Nome obrigatório, tipo conhecido, material coerente com o tipo (liga existente em metal, texto livre só na pedra) e
    /// peso positivo. Com <paramref name="soLigaAtiva"/> (peça nova), a liga precisa estar ativa; na alteração, a liga
    /// já gravada pode estar desativada e o domínio recusa trocar para uma desativada.
    /// </summary>
    public static void ValidarDadosDaPeca<T>(this AbstractValidator<T> validator, IUnitOfWork unitOfWork, bool soLigaAtiva)
        where T : IDadosDaPeca
    {
        validator.RuleFor(c => c.Nome).NotEmpty().MaximumLength(Peca.NomeMaxLength).WithName("Nome");
        validator.RuleFor(c => c.Tipo).IsInEnum().WithName("Tipo");
        validator.RuleFor(c => c.Descricao).MaximumLength(Peca.DescricaoMaxLength).WithName("Descrição");

        validator.RuleFor(c => c.Material)
            .MaximumLength(Peca.MaterialMaxLength)
            .When(c => c.Tipo == TipoPeca.Pedra)
            .WithName("Material");
        validator.RuleFor(c => c.Material)
            .Must(string.IsNullOrWhiteSpace).WithMessage("Material livre só na pedra. Nas outras peças, escolha a liga.")
            .When(c => c.Tipo != TipoPeca.Pedra)
            .WithName("Material");

        validator.RuleFor(c => c.LigaId)
            .Null().WithMessage("Pedra não usa liga. Informe o material.")
            .When(c => c.Tipo == TipoPeca.Pedra)
            .WithName("Liga");
        if (soLigaAtiva)
        {
            validator.RuleFor(c => c.LigaId)
                .MustExists<T, Liga>(unitOfWork).WithMessage("Liga não encontrada ou desativada.")
                .When(c => c.Tipo != TipoPeca.Pedra)
                .WithName("Liga");
        }
        else
        {
            validator.RuleFor(c => c.LigaId)
                .MustAsync((id, cancellationToken) => id is null ? Task.FromResult(true) : unitOfWork.GetRepository<Liga>().ExisteAsync(id.Value, cancellationToken))
                .WithMessage("Liga não encontrada.")
                .When(c => c.Tipo != TipoPeca.Pedra)
                .WithName("Liga");
        }

        validator.RuleFor(c => c.PesoGramas)
            .GreaterThan(0)
            .LessThanOrEqualTo(PesoMaximoEmGramas)
            .PrecisionScale(10, 3, ignoreTrailingZeros: true)
            .When(c => c.PesoGramas is not null)
            .WithName("Peso");
    }

    /// <summary>
    /// Valor (custo de uma unidade) da peça do projeto: não negativo, em centavos e até <see cref="ValorMaximo"/>.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal?> ValorDaPeca<T>(this IRuleBuilder<T, decimal?> regra)
        => regra
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ValorMaximo)
            .PrecisionScale(18, Dinheiro.CasasDecimais, ignoreTrailingZeros: true)
            .WithName("Valor");
}
