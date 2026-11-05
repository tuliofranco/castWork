using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.RemoverItem;

[Permissao(Regras.Projetos.Editar)]
public sealed class RemoverItemCommand : ComandoDeVersao, IRequest
{
    public long ItemId { get; set; }
}

public sealed class RemoverItemCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RemoverItemCommand>
{
    public async Task Handle(RemoverItemCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.RemoverItem(request.VersaoId, request.ItemId);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
