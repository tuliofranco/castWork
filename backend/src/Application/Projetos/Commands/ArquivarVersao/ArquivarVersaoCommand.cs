using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.ArquivarVersao;

/// <summary>
/// Arquiva uma versão em desenvolvimento. Versão publicada não pode ser arquivada.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ArquivarVersaoCommand : ComandoDeVersao, IRequest;

public sealed class ArquivarVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ArquivarVersaoCommand>
{
    public async Task Handle(ArquivarVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.ArquivarVersao(request.VersaoId);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class ArquivarVersaoCommandValidator : Common.Validators.ValidatorBase<ArquivarVersaoCommand>
{
    public ArquivarVersaoCommandValidator(IUnitOfWork unitOfWork)
        : base(unitOfWork)
        => this.ValidarProjeto(UnitOfWork);
}
