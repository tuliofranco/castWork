using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DesarquivarVersao;

/// <summary>
/// Devolve uma versão arquivada para desenvolvimento.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class DesarquivarVersaoCommand : ComandoDeVersao, IRequest;

public sealed class DesarquivarVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DesarquivarVersaoCommand>
{
    public async Task Handle(DesarquivarVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.DesarquivarVersao(request.VersaoId);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class DesarquivarVersaoCommandValidator : Common.Validators.ValidatorBase<DesarquivarVersaoCommand>
{
    public DesarquivarVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
