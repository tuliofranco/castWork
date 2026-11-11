using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.PublicarPecaDoProjeto;

/// <summary>
/// Publica a peça do projeto em rascunho: cria a peça na biblioteca com os dados dela (sem valor; a liga vai com o Id e a
/// cópia do nome e do teor) e o desenho (o arquivo final do desenho 2D e do modelo 3D da peça; sem final, a ferramenta não vai, com as imagens guardadas) e vincula as duas, numa
/// transação. É a única forma de povoar a biblioteca pela tela. Retorna o Id da peça nova da biblioteca.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class PublicarPecaDoProjetoCommand : ComandoDePecaDoProjeto, IRequest<long>;

public sealed class PublicarPecaDoProjetoCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio) : IRequestHandler<PublicarPecaDoProjetoCommand, long>
{
    public async Task<long> Handle(PublicarPecaDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var publicada = projeto.PublicarPeca(request.PecaDoProjetoId);
        var agora = relogio.GetUtcNow();
        foreach (var desenho in await DesenhosDaPecaDoProjeto.FinaisAsync(unitOfWork, request.PecaDoProjetoId, cancellationToken))
        {
            publicada.GuardarDesenho(desenho.Ferramenta, desenho.Conteudo, agora, desenho.Imagens);
        }

        unitOfWork.GetRepository<Peca>().Adicionar(publicada);
        await unitOfWork.SalvarAsync(cancellationToken);
        return publicada.Id;
    }
}
