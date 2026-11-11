using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Projetos;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Projetos.Commands.RenomearProjeto;

/// <summary>
/// Troca o nome do projeto.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class RenomearProjetoCommand : ComandoDeProjeto, IRequest
{
    public string Nome { get; set; } = string.Empty;
}

public sealed class RenomearProjetoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RenomearProjetoCommand>
{
    public async Task Handle(RenomearProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.Renomear(request.Nome);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class RenomearProjetoCommandValidator : ValidatorBase<RenomearProjetoCommand>
{
    public RenomearProjetoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
    {
        this.ValidarProjeto(UnitOfWork);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(Projeto.NomeMaxLength).WithName("Nome");
    }
}
