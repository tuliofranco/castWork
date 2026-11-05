using Castwork.Application.Abstractions;
using Castwork.Application.Common.Tempo;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Pecas;

using FluentValidation;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

/// <summary>
/// Espelha as invariantes do domínio com mensagens por campo para o front.
/// Com o cascade global em Stop, cada campo para no primeiro erro (não consulta o banco se o Id for 0).
/// </summary>
public sealed class CriarPecaCommandValidator : ValidatorBase<CriarPecaCommand>
{
    public CriarPecaCommandValidator(IUnitOfWork unitOfWork, TimeProvider timeProvider)
        : base(unitOfWork)
    {
        RuleFor(c => c.ClienteId)
            .GreaterThan(0).WithMessage("Informe o cliente.")
            .MustExists<CriarPecaCommand, Cliente>(UnitOfWork)
            .WithName("Cliente");

        RuleFor(c => c.Titulo)
            .NotEmpty()
            .MaximumLength(Peca.TituloMaxLength)
            .WithName("Título");

        RuleFor(c => c.Descricao)
            .MaximumLength(Peca.DescricaoMaxLength)
            .WithName("Descrição");

        RuleFor(c => c.PrazoEntrega)
            .Must(prazo => prazo >= timeProvider.HojeNoBrasil())
            .When(c => c.PrazoEntrega is not null)
            .WithMessage("O prazo de entrega não pode estar no passado.")
            .WithName("Prazo de entrega");
    }
}
