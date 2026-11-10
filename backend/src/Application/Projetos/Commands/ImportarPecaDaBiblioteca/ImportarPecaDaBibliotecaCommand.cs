using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.ImportarPecaDaBiblioteca;

/// <summary>
/// Traz para o projeto uma cópia da peça da biblioteca: publicada, vinculada a ela, com a mesma liga (Id e cópia do nome e
/// do teor), sem valor (o projeto define o seu) e com uma cópia do desenho dela (2D e 3D), que vira o primeiro arquivo das
/// ferramentas da peça no projeto. Retorna o Id da peça do projeto.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ImportarPecaDaBibliotecaCommand : ComandoDeProjeto, IRequest<long>
{
    /// <summary>
    /// A peça da biblioteca (ativa).
    /// </summary>
    public long PecaId { get; set; }
}

public sealed class ImportarPecaDaBibliotecaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio) : IRequestHandler<ImportarPecaDaBibliotecaCommand, long>
{
    public async Task<long> Handle(ImportarPecaDaBibliotecaCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);

        // O agregado vem com os desenhos (os filhos da peça).
        var pecaDaBiblioteca = await unitOfWork.GetRepository<Peca>().ObterPorIdOuFalharAsync(request.PecaId, cancellationToken);
        var peca = projeto.ImportarPeca(pecaDaBiblioteca);
        await unitOfWork.SalvarAsync(cancellationToken);

        // A conversa de cada ferramenta aponta para a peça do projeto: só depois de ela ter Id.
        await DesenhosDaPecaDoProjeto.CopiarDaBibliotecaAsync(unitOfWork, projeto.Id, peca.Id, pecaDaBiblioteca.Desenhos, relogio.GetUtcNow(), cancellationToken);
        return peca.Id;
    }
}
