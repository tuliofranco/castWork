using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.DefinirMetaisDaVersao;

/// <summary>
/// Orçamento da versão: as ligas da peça e o peso estimado de cada uma (por enquanto digitado). Substitui a lista;
/// a liga que continua mantém o peso real registrado. Só em versão em desenvolvimento.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class DefinirMetaisDaVersaoCommand : ComandoDeVersao, IRequest
{
    public IReadOnlyList<MetalInformado> Metais { get; set; } = [];
}

public sealed class MetalInformado
{
    public long LigaId { get; set; }

    /// <summary>
    /// Peso estimado da peça pronta nesta liga, em gramas (sem a perda de fundição).
    /// </summary>
    public decimal PesoGramas { get; set; }
}

public sealed class DefinirMetaisDaVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DefinirMetaisDaVersaoCommand>
{
    public async Task Handle(DefinirMetaisDaVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.DefinirMetaisDaVersao(request.VersaoId, request.Metais.Select(m => (m.LigaId, Peso.EmGramas(m.PesoGramas))));
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
