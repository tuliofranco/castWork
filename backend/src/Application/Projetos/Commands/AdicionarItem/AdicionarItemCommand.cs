using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.AdicionarItem;

/// <summary>
/// Adiciona uma peça do projeto à composição da versão. Se ela já estiver lá, soma a quantidade.
/// Retorna o Id do item.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class AdicionarItemCommand : ComandoDeVersao, IRequest<long>
{
    /// <summary>
    /// Peça ativa deste projeto (a de outro projeto ou da biblioteca não entra).
    /// </summary>
    public long PecaDoProjetoId { get; set; }

    public int Quantidade { get; set; } = 1;

    public string? Observacao { get; set; }
}

public sealed class AdicionarItemCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AdicionarItemCommand, long>
{
    public async Task<long> Handle(AdicionarItemCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var item = projeto.AdicionarItem(request.VersaoId, request.PecaDoProjetoId, request.Quantidade, request.Observacao);
        await unitOfWork.SalvarAsync(cancellationToken);
        return item.Id;
    }
}
