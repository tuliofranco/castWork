using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

/// <summary>
/// Cadastra uma peça na biblioteca. Retorna o Id. O custo só é gravado para quem tem <c>precificacao.ler</c>.
/// </summary>
[Permissao(Regras.Pecas.Criar)]
public sealed class CriarPecaCommand : DadosDaPeca, IRequest<long>
{
}

public sealed class CriarPecaCommandHandler(IUnitOfWork unitOfWork, ISessaoService sessao, IPermissaoService permissaoService)
    : IRequestHandler<CriarPecaCommand, long>
{
    public async Task<long> Handle(CriarPecaCommand request, CancellationToken cancellationToken)
    {
        var podeVerCusto = await permissaoService.SessaoPossuiRegraAsync(sessao, Regras.Precificacao.Consultar, cancellationToken);
        var peca = Peca.Criar(
            request.Nome,
            request.Tipo,
            request.Material,
            request.TeorDominio(),
            request.PesoDominio(),
            request.Descricao,
            podeVerCusto ? request.CustoDominio() : null);

        unitOfWork.GetRepository<Peca>().Adicionar(peca);
        await unitOfWork.SalvarAsync(cancellationToken);

        return peca.Id;
    }
}
