using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Projetos.Commands.RegistrarPesoReal;

/// <summary>
/// Peso medido na peça pronta, para uma liga da versão (nulo apaga). Não muda o preço: é conferência do estimado.
/// Aceito em qualquer status, inclusive publicada e arquivada.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class RegistrarPesoRealCommand : ComandoDeVersao, IRequest
{
    public long LigaId { get; set; }

    public decimal? PesoGramas { get; set; }
}

public sealed class RegistrarPesoRealCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RegistrarPesoRealCommand>
{
    public async Task Handle(RegistrarPesoRealCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        projeto.RegistrarPesoReal(request.VersaoId, request.LigaId, request.PesoGramas is decimal gramas ? Peso.EmGramas(gramas) : null);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
