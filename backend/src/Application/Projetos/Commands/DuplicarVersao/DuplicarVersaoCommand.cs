using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DuplicarVersao;

/// <summary>
/// Copia a composição de uma versão (inclusive publicada) para uma nova versão em desenvolvimento.
/// Retorna o Id da versão nova.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class DuplicarVersaoCommand : ComandoDeVersao, IRequest<long>
{
    /// <summary>
    /// Opcional: sem nome, vira "Versão N".
    /// </summary>
    public string? Nome { get; set; }
}

public sealed class DuplicarVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DuplicarVersaoCommand, long>
{
    public async Task<long> Handle(DuplicarVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var copia = projeto.DuplicarVersao(request.VersaoId, request.Nome);
        await unitOfWork.SalvarAsync(cancellationToken);
        return copia.Id;
    }
}
