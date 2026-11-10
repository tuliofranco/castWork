using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Precificacao;

using FluentValidation;

using MediatR;

namespace Castwork.Application.Precificacao.Commands.DesativarLiga;

/// <summary>
/// Desativa uma liga: some da tela e dos orçamentos novos. Não há exclusão, porque a liga pode estar em uso.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DesativarLigaCommand : IRequest
{
    public long LigaId { get; set; }
}

public sealed class DesativarLigaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DesativarLigaCommand>
{
    public async Task Handle(DesativarLigaCommand request, CancellationToken cancellationToken)
    {
        var liga = await unitOfWork.GetRepository<Liga>().ObterPorIdOuFalharAsync(request.LigaId, cancellationToken);
        liga.Desativar();
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class DesativarLigaCommandValidator : ValidatorBase<DesativarLigaCommand>
{
    public DesativarLigaCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => RuleFor(c => c.LigaId).GreaterThan(0).MustExists<DesativarLigaCommand, Liga>(UnitOfWork).WithName("Liga");
}
