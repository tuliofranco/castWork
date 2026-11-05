using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.CriarVersao;

/// <summary>
/// Nova versão vazia, com o próximo número. Retorna o Id da versão.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class CriarVersaoCommand : ComandoDeProjeto, IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public string? Observacoes { get; set; }
}

public sealed class CriarVersaoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarVersaoCommand, long>
{
    public async Task<long> Handle(CriarVersaoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var versao = projeto.CriarVersao(request.Nome, request.Observacoes);
        await unitOfWork.SalvarAsync(cancellationToken);
        return versao.Id;
    }
}
