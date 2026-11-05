using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.PublicarVersao;

/// <summary>
/// Congela a composição da versão (mesmo vazia).
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class PublicarVersaoCommand : ComandoDeVersao, IRequest;

public sealed class PublicarVersaoCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<PublicarVersaoCommand>
{
    public async Task Handle(PublicarVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.PublicarVersao(request.VersaoId, timeProvider.GetUtcNow());
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
