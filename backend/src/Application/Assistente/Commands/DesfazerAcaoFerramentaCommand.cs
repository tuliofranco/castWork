using Castwork.Application.Abstractions;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Assistente.Commands;

/// <summary>
/// Desfaz a última ação da IA: o arquivo de antes dela volta a ser o atual (gravado de novo, o histórico
/// só cresce) e o cartão da mensagem fica como desfeito.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class DesfazerAcaoFerramentaCommand : ComandoDeFerramenta, IRequest<ConversaFerramentaDto>
{
    public int Ordem { get; set; }
}

public sealed class DesfazerAcaoFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<DesfazerAcaoFerramentaCommand, ConversaFerramentaDto>
{
    public async Task<ConversaFerramentaDto> Handle(DesfazerAcaoFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Conversa", request.Ferramenta);

        var agora = relogio.GetUtcNow();
        if (conversa.AguardandoIa(agora))
        {
            throw new RegraDeNegocioException("Espere a IA terminar antes de desfazer.");
        }

        var antesId = conversa.DesfazerAcao(request.Ordem);
        var arquivos = unitOfWork.GetRepository<ArquivoFerramenta>();

        // Sem arquivo de antes (a ação criou o primeiro), volta ao desenho em branco. Nada é apagado: o que
        // o joalheiro salvou depois continua no histórico.
        var restaurado = antesId > 0
            ? (await arquivos.FindBy(a => a.Id == antesId && a.ConversaId == conversa.Id).AsNoTracking().SingleOrDefaultAsync(cancellationToken)
                    ?? throw new RegraDeNegocioException("O desenho de antes desta ação foi apagado: não dá para desfazer."))
                .Copiar(OrigemArquivo.Desfazer, agora)
            : ArquivoFerramenta.EmBrancoPara(conversa.Id, agora);
        arquivos.Adicionar(restaurado);
        await unitOfWork.SalvarAsync(cancellationToken);
        var arquivoId = restaurado.EmBranco ? null : (long?)restaurado.Id;

        return GetConversaFerramentaQueryHandler.Mapear(conversa, arquivoId, processando: false);
    }
}

public sealed class DesfazerAcaoFerramentaCommandValidator : ValidatorBase<DesfazerAcaoFerramentaCommand>
{
    public DesfazerAcaoFerramentaCommandValidator(IUnitOfWork unitOfWork, CatalogoDeFerramentas catalogo)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, catalogo);
}
