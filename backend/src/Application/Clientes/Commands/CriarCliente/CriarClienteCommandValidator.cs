using Castwork.Application.Abstractions;
using Castwork.Application.Common.Enderecos;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.ValueObjects;

using FluentValidation;

namespace Castwork.Application.Clientes.Commands.CriarCliente;

public sealed class CriarClienteCommandValidator : ValidatorBase<CriarClienteCommand>
{
    public CriarClienteCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome)
            .NotEmpty()
            .MaximumLength(Cliente.NomeMaxLength)
            .WithName("Nome");

        RuleFor(c => c.Documento!)
            .Must(Documento.EhValido).WithMessage("CPF/CNPJ inválido.")
            .MustAsync(SerDocumentoUnicoAsync).WithMessage("Já existe um cliente com este CPF/CNPJ.")
            .When(c => !string.IsNullOrWhiteSpace(c.Documento))
            .WithName("CPF/CNPJ");

        RuleFor(c => c.Email!)
            .EmailAddress()
            .MaximumLength(Cliente.EmailMaxLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Email))
            .WithName("E-mail");

        RuleFor(c => c.Telefone)
            .MaximumLength(Cliente.TelefoneMaxLength)
            .WithName("Telefone");

        RuleFor(c => c.Observacoes)
            .MaximumLength(Cliente.ObservacoesMaxLength)
            .WithName("Observações");

        RuleFor(c => c.Endereco!)
            .SetValidator(new EnderecoDtoValidator())
            .When(c => c.Endereco is not null);
    }

    private async Task<bool> SerDocumentoUnicoAsync(string documento, CancellationToken cancellationToken)
    {
        var numero = Documento.Criar(documento);
        return !await UnitOfWork.GetRepository<Cliente>().FindBy(c => c.Documento == numero).AnyAsync(cancellationToken);
    }
}
