using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Ferramentas;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// Uma versão do histórico do arquivo da ferramenta, para baixar. Nulo se for a versão em branco.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetVersaoArquivoFerramentaQuery : IRequest<ArquivoDaFerramentaDto?>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public long ArquivoId { get; set; }
}

public sealed class GetVersaoArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetVersaoArquivoFerramentaQuery, ArquivoDaFerramentaDto?>
{
    public async Task<ArquivoDaFerramentaDto?> Handle(GetVersaoArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        var arquivo = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.Id == request.ArquivoId && a.ConversaId == conversa.Id)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Arquivo", request.ArquivoId);
        return arquivo.EmBranco ? null : new ArquivoDaFerramentaDto(arquivo.Id, arquivo.Conteudo);
    }
}
