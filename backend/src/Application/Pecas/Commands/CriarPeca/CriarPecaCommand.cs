using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

/// <summary>
/// Cadastra uma peça na biblioteca, sem valor. Retorna o Id. Continua na API, mas a tela não o usa: a biblioteca é
/// povoada pela publicação das peças do projeto.
/// </summary>
[Permissao(Regras.Pecas.Criar)]
public sealed class CriarPecaCommand : DadosDaPeca, IRequest<long>
{
}

public sealed class CriarPecaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarPecaCommand, long>
{
    public async Task<long> Handle(CriarPecaCommand request, CancellationToken cancellationToken)
    {
        var liga = await request.ObterLigaAsync(unitOfWork, cancellationToken);
        var peca = Peca.Criar(request.Nome, request.Tipo, liga, request.Material, request.PesoDominio(), request.Descricao);

        unitOfWork.GetRepository<Peca>().Adicionar(peca);
        await unitOfWork.SalvarAsync(cancellationToken);

        return peca.Id;
    }
}
