using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DefinirMarkupDaVersao;

/// <summary>
/// Markup só desta versão (1,8 = custo × 1,8). Nulo volta a valer o do projeto. Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DefinirMarkupDaVersaoCommand : ComandoDeVersao, IRequest
{
    public decimal? Markup { get; set; }
}

public sealed class DefinirMarkupDaVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirMarkupDaVersaoCommand>
{
    public async Task Handle(DefinirMarkupDaVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.DefinirMarkupDaVersao(request.VersaoId, request.Markup);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
