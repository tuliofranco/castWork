using Castwork.Application.Abstractions;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

namespace Castwork.Application.Projetos.Commands.CriarProjeto;

public sealed class CriarProjetoCommandValidator : ValidatorBase<CriarProjetoCommand>
{
    public CriarProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(Projeto.NomeMaxLength).WithName("Nome");
        RuleFor(c => c.Descricao).MaximumLength(Projeto.DescricaoMaxLength).WithName("Descrição");
        RuleFor(c => c.ClienteId).MustExists<CriarProjetoCommand, Cliente>(UnitOfWork).WithName("Cliente");
    }
}
