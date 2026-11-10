using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Commands.AlterarPeca;

/// <summary>
/// Altera uma peça ativa da biblioteca. O Id vem da rota. Custo nulo deixa a peça sem custo; para quem não tem
/// <c>precificacao.ler</c> o custo é ignorado e o atual é mantido.
/// </summary>
[Permissao(Regras.Pecas.Alterar)]
public sealed class AlterarPecaCommand : DadosDaPeca, IRequest
{
    public long PecaId { get; set; }
}

public sealed class AlterarPecaCommandHandler(IUnitOfWork unitOfWork, ISessaoService sessao, IPermissaoService permissaoService)
    : IRequestHandler<AlterarPecaCommand>
{
    public async Task Handle(AlterarPecaCommand request, CancellationToken cancellationToken)
    {
        var peca = await unitOfWork.GetRepository<Peca>().ObterPorIdOuFalharAsync(request.PecaId, cancellationToken);
        peca.Atualizar(request.Nome, request.Tipo, request.Material, request.TeorDominio(), request.PesoDominio(), request.Descricao);

        if (await permissaoService.SessaoPossuiRegraAsync(sessao, Regras.Precificacao.Consultar, cancellationToken))
        {
            peca.DefinirCusto(request.CustoDominio());
        }

        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
