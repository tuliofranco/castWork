using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Ferramentas;

using MediatR;

namespace Castwork.Application.Ferramentas.Commands;

/// <summary>
/// Apaga as mensagens e as imagens da conversa da ferramenta. O arquivo (o desenho) fica como está.
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
        if (await ConversaDaVersao.ObterAsync(unitOfWork, request.VersaoId, request.Ferramenta, cancellationToken) is not { } conversa)
        {
            return;
        }

        conversa.Apagar();
        await unitOfWork.GetRepository<ImagemConversa>().FindBy(i => i.ConversaId == conversa.Id).ExecuteDeleteAsync(cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}

public sealed class ApagarConversaFerramentaCommandValidator : ValidatorBase<ApagarConversaFerramentaCommand>
{
    public ApagarConversaFerramentaCommandValidator(IUnitOfWork unitOfWork, IAssistenteFerramenta assistente)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, assistente);
}
