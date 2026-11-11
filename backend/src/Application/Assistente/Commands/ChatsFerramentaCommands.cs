using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Encerra o chat em andamento (vai para o histórico) e começa um em branco, no mesmo desenho.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class NovoChatFerramentaCommand : ComandoDeFerramenta, IRequest<ConversaFerramentaDto>
{
}

/// <summary>
/// Retoma um chat anterior: ele volta a ser o que vai para a IA.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class ContinuarChatFerramentaCommand : ComandoDeFerramenta, IRequest<ConversaFerramentaDto>
{
    public int Numero { get; set; }
}

public sealed class NovoChatFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<NovoChatFerramentaCommand, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(NovoChatFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var agora = relogio.GetUtcNow();
        var conversa = await ConversaDoEscopo.ObterOuCriarAsync(unitOfWork, request, request.Ferramenta, agora, cancellationToken);
        conversa.NovoChat(agora);
        await unitOfWork.SalvarAsync(cancellationToken);
        return await ChatsDaConversa.ComArquivoAsync(unitOfWork, conversa, cancellationToken);
    }
}

public sealed class ContinuarChatFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<ContinuarChatFerramentaCommand, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(ContinuarChatFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken)
            ?? throw new Domain.Exceptions.EntidadeNaoEncontradaException("Chat", request.Numero);
        conversa.ContinuarChat(request.Numero, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(cancellationToken);
        return await ChatsDaConversa.ComArquivoAsync(unitOfWork, conversa, cancellationToken);
    }
}

public sealed class NovoChatFerramentaCommandValidator : ValidatorBase<NovoChatFerramentaCommand>
{
    public NovoChatFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}

public sealed class ContinuarChatFerramentaCommandValidator : ValidatorBase<ContinuarChatFerramentaCommand>
{
    public ContinuarChatFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}

/// <summary>
/// O chat em andamento com o arquivo atual, como o front lê depois de trocar de chat.
/// </summary>
internal static class ChatsDaConversa
{
    public static async Task<ConversaFerramentaDto> ComArquivoAsync(IUnitOfWork unitOfWork, ConversaFerramenta conversa, CancellationToken cancellationToken)
    {
        var arquivo = await ConversaDoEscopo.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        return GetConversaFerramentaQueryHandler.Mapear(conversa, arquivo is { EmBranco: false } ? arquivo.Id : null, processando: false);
    }
}
