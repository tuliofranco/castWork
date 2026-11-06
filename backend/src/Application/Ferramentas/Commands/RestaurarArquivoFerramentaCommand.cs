using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Ferramentas.Commands;

/// <summary>
/// Volta a uma versão do histórico: grava uma cópia dela como a atual (o histórico só cresce). Retorna o Id
/// da nova versão do arquivo, ou nulo se a versão restaurada era o desenho em branco.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class RestaurarArquivoFerramentaCommand : ComandoDeFerramenta, IRequest<long?>
{
    public long ArquivoId { get; set; }
}

public sealed class RestaurarArquivoFerramentaCommandHandler(IUnitOfWork unitOfWork, TimeProvider relogio)
    : IRequestHandler<RestaurarArquivoFerramentaCommand, long?>
{
    public async Task<long?> Handle(RestaurarArquivoFerramentaCommand request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.VersaoId, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        var arquivos = unitOfWork.GetRepository<ArquivoFerramenta>();
        var origem = await arquivos.FindBy(a => a.Id == request.ArquivoId && a.ConversaId == conversa.Id)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);

        var copia = origem.Copiar(OrigemArquivo.Restauracao, relogio.GetUtcNow());
        arquivos.Adicionar(copia);
        await unitOfWork.SalvarAsync(cancellationToken);
        return copia.EmBranco ? null : copia.Id;
    }
}

public sealed class RestaurarArquivoFerramentaCommandValidator : ValidatorBase<RestaurarArquivoFerramentaCommand>
{
    public RestaurarArquivoFerramentaCommandValidator(IUnitOfWork unitOfWork, IAssistenteFerramenta assistente)
        : base(unitOfWork) => this.ValidarFerramenta(UnitOfWork, assistente);
}
