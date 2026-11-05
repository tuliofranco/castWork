using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Common.Enderecos;

public sealed class EnderecoDtoValidator : AbstractValidator<EnderecoDto>
{
    public EnderecoDtoValidator()
    {
        RuleFor(e => e.Cep)
            .Must(cep => cep.Count(char.IsAsciiDigit) == 8 && cep.All(c => char.IsAsciiDigit(c) || c is '-' or '.'))
            .WithMessage("CEP deve ter 8 dígitos.");
        RuleFor(e => e.Logradouro).NotEmpty().MaximumLength(Endereco.LogradouroMaxLength).WithName("Logradouro");
        RuleFor(e => e.Numero).NotEmpty().MaximumLength(Endereco.NumeroMaxLength).WithName("Número");
        RuleFor(e => e.Complemento).MaximumLength(Endereco.ComplementoMaxLength).WithName("Complemento");
        RuleFor(e => e.Bairro).NotEmpty().MaximumLength(Endereco.BairroMaxLength).WithName("Bairro");
        RuleFor(e => e.Cidade).NotEmpty().MaximumLength(Endereco.CidadeMaxLength).WithName("Cidade");
        RuleFor(e => e.Uf).Must(Endereco.EhUfValida).WithMessage("UF inválida.");
    }
}
