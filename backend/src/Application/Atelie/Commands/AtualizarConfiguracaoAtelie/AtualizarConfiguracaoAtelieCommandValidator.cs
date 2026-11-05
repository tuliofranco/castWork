using Castwork.Application.Abstractions;
using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Atelie.Commands.AtualizarConfiguracaoAtelie;

public sealed class AtualizarConfiguracaoAtelieCommandValidator : ValidatorBase<AtualizarConfiguracaoAtelieCommand>
{
    public const decimal ValorHoraMaximo = 10_000m;

    public AtualizarConfiguracaoAtelieCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(ConfiguracaoAtelie.NomeMaxLength).WithName("Nome do ateliê");

        RuleFor(c => c.Documento!)
            .Must(Documento.EhValido).WithMessage("CPF/CNPJ inválido.")
            .When(c => !string.IsNullOrWhiteSpace(c.Documento))
            .WithName("CPF/CNPJ");

        RuleFor(c => c.Email!)
            .EmailAddress()
            .MaximumLength(ConfiguracaoAtelie.EmailMaxLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Email))
            .WithName("E-mail");

        RuleFor(c => c.Telefone).MaximumLength(ConfiguracaoAtelie.TelefoneMaxLength).WithName("Telefone");

        RuleFor(c => c.ValorHoraPadrao)
            .InclusiveBetween(0, ValorHoraMaximo)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithName("Valor da hora");

        RuleFor(c => c.Endereco!).SetValidator(new EnderecoDtoValidator()).When(c => c.Endereco is not null);
    }
}
