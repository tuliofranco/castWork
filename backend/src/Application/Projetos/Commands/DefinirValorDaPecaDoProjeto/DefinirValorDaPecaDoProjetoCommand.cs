using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DefinirValorDaPecaDoProjeto;

/// <summary>
/// Define o valor da peça neste projeto: o custo de uma unidade (compra ou produção, não o preço de venda). Nulo tira o
/// valor. Não muda o status nem o vínculo com a biblioteca.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DefinirValorDaPecaDoProjetoCommand : ComandoDePecaDoProjeto, IRequest
{
    public decimal? Valor { get; set; }
}

public sealed class DefinirValorDaPecaDoProjetoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirValorDaPecaDoProjetoCommand>
{
    public async Task Handle(DefinirValorDaPecaDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.DefinirValorDaPeca(request.PecaDoProjetoId, request.Valor is decimal valor ? Dinheiro.Criar(valor) : null);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
