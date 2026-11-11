using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Commands.AlterarPeca;

/// <summary>
/// Altera uma peça ativa da biblioteca. O Id vem da rota. Continua na API, mas a tela não o usa: o projeto nunca
/// altera a peça que publicou (alterar a cópia dele cria outra peça).
/// </summary>
[Permissao(Regras.Pecas.Alterar)]
public sealed class AlterarPecaCommand : DadosDaPeca, IRequest
{
    public long PecaId { get; set; }
}

public sealed class AlterarPecaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AlterarPecaCommand>
{
    public async Task Handle(AlterarPecaCommand request, CancellationToken cancellationToken)
    {
        var peca = await unitOfWork.GetRepository<Peca>().ObterPorIdOuFalharAsync(request.PecaId, cancellationToken);
        var liga = await request.ObterLigaAsync(unitOfWork, cancellationToken);
        peca.Atualizar(request.Nome, request.Tipo, liga, request.Material, request.PesoDominio(), request.Descricao);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
