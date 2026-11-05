using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;

using MediatR;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

/// <summary>
/// Abre uma peça em briefing. Retorna o Id da peça criada.
/// </summary>
[Permissao(Regras.Pecas.Criar)]
public sealed class CriarPecaCommand : IRequest<long>
{
    public long ClienteId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public DateOnly? PrazoEntrega { get; set; }
}

public sealed class CriarPecaCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : IRequestHandler<CriarPecaCommand, long>
{
    public async Task<long> Handle(CriarPecaCommand request, CancellationToken cancellationToken)
    {
        var peca = Peca.Criar(request.ClienteId, request.Titulo, request.Descricao, request.PrazoEntrega, timeProvider.GetUtcNow());

        unitOfWork.GetRepository<Peca>().Adicionar(peca);
        await unitOfWork.SalvarAsync(cancellationToken);

        return peca.Id;
    }
}
