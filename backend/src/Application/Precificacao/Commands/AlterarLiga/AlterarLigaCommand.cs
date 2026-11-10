using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Precificacao.Commands.AlterarLiga;

/// <summary>
/// Altera nome, metal, teor ou densidade de uma liga ativa (inclusive as pré-cadastradas). O Id vem da rota.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class AlterarLigaCommand : DadosDaLiga, IRequest
{
    public long LigaId { get; set; }
}

public sealed class AlterarLigaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AlterarLigaCommand>
{
    public async Task Handle(AlterarLigaCommand request, CancellationToken cancellationToken)
    {
        var liga = await unitOfWork.GetRepository<Liga>().ObterPorIdOuFalharAsync(request.LigaId, cancellationToken);
        liga.Alterar(request.Nome, request.Metal, TeorMetal.Parse(request.Teor), request.Densidade);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
