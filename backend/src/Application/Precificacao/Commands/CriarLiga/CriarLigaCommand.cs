using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Precificacao.Commands.CriarLiga;

/// <summary>
/// Cadastra uma liga do ateliê. Retorna o Id.
/// </summary>
[Permissao(Regras.Precificacao.Editar)]
public sealed class CriarLigaCommand : DadosDaLiga, IRequest<long>;

public sealed class CriarLigaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarLigaCommand, long>
{
    public async Task<long> Handle(CriarLigaCommand request, CancellationToken cancellationToken)
    {
        var liga = Liga.Criar(request.Nome, request.Metal, TeorMetal.Parse(request.Teor), request.Densidade);

        unitOfWork.GetRepository<Liga>().Adicionar(liga);
        await unitOfWork.SalvarAsync(cancellationToken);

        return liga.Id;
    }
}
