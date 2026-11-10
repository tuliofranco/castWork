using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Precificacao.Commands.AtualizarCotacao;

/// <summary>
/// Registra a cotação do metal puro (R$/g de ouro 24k ou prata fina). Vira uma linha nova no histórico, com data e
/// autor; a anterior continua lá. Retorna o Id da cotação.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class AtualizarCotacaoCommand : IRequest<long>
{
    public Metal Metal { get; set; }

    /// <summary>
    /// R$ por grama do metal puro.
    /// </summary>
    public decimal Valor { get; set; }
}

public sealed class AtualizarCotacaoCommandHandler(IUnitOfWork unitOfWork, ISessaoService sessao, TimeProvider timeProvider)
    : IRequestHandler<AtualizarCotacaoCommand, long>
{
    public async Task<long> Handle(AtualizarCotacaoCommand request, CancellationToken cancellationToken)
    {
        var cotacao = CotacaoMetal.Registrar(request.Metal, Dinheiro.Criar(request.Valor), timeProvider.GetUtcNow(), sessao.UsuarioId);

        unitOfWork.GetRepository<CotacaoMetal>().Adicionar(cotacao);
        await unitOfWork.SalvarAsync(cancellationToken);

        return cotacao.Id;
    }
}
