using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Precificacao;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.CriarProjeto;

/// <summary>
/// Cria o projeto, sem versões, com uma cópia dos preços do ateliê (cotações vigentes e parâmetros). Retorna o Id.
/// </summary>
[Permissao(Regras.Projetos.Criar)]
public sealed class CriarProjetoCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public long? ClienteId { get; set; }
}

public sealed class CriarProjetoCommandHandler(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<CriarProjetoCommand, long>
{
    public async Task<long> Handle(CriarProjetoCommand request, CancellationToken cancellationToken)
    {
        var precosDoAtelie = await unitOfWork.ObterPrecosDoAtelieAsync(cancellationToken);
        var projeto = Projeto.Criar(request.Nome, request.Descricao, request.ClienteId, precosDoAtelie, timeProvider.GetUtcNow());

        unitOfWork.GetRepository<Projeto>().Adicionar(projeto);
        await unitOfWork.SalvarAsync(cancellationToken);

        return projeto.Id;
    }
}
