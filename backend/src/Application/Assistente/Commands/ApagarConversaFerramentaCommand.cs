using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Apaga o chat em andamento (mensagens e imagens). Os chats anteriores e o arquivo (o desenho) ficam.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ApagarConversaFerramentaCommand : ComandoDeFerramenta, IRequest
{
}

public sealed class ApagarConversaFerramentaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ApagarConversaFerramentaCommand>
{
    public async Task Handle(ApagarConversaFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        if (await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken) is not { } conversa)
        {
            return;
        }

        // As imagens do chat apagado saem junto; as dos outros chats continuam (eles ficam no histórico).
        var chaves = conversa.MensagensDoChat.SelectMany(m => m.Imagens).ToList();
        conversa.Apagar();
        await unitOfWork.GetRepository<ImagemConversa>().FindBy(i => i.ConversaId == conversa.Id && chaves.Contains(i.Chave)).ExecuteDeleteAsync(cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class ApagarConversaFerramentaCommandValidator : ValidatorBase<ApagarConversaFerramentaCommand>
{
    public ApagarConversaFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}
