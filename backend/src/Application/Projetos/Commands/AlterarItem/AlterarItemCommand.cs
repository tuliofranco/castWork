using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.AlterarItem;

[Permissao(Regras.Projetos.Editar)]
public sealed class AlterarItemCommand : ComandoDeVersao, IRequest
{
    public long ItemId { get; set; }

    public int Quantidade { get; set; }

    public string? Observacao { get; set; }
}

public sealed class AlterarItemCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AlterarItemCommand>
{
    public async Task Handle(AlterarItemCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.AlterarItem(request.VersaoId, request.ItemId, request.Quantidade, request.Observacao);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
