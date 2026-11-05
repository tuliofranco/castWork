using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.CriarProjeto;

/// <summary>
/// Cria o projeto já com a Versão 1 em desenvolvimento. Retorna o Id do projeto.
/// </summary>
[Permissao(Regras.Projetos.Criar)]
public sealed class CriarProjetoCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public long? ClienteId { get; set; }
}

public sealed class CriarProjetoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarProjetoCommand, long>
{
    public async Task<long> Handle(CriarProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = Projeto.Criar(request.Nome, request.Descricao, request.ClienteId);

        unitOfWork.GetRepository<Projeto>().Adicionar(projeto);
        await unitOfWork.SalvarAsync(cancellationToken);

        return projeto.Id;
    }
}
