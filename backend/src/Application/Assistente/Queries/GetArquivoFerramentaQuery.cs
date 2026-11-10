using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// O arquivo atual da ferramenta (o DXF do desenho) ou, com <see cref="Final"/>, o final da peça, ou nulo se ainda não existe.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetArquivoFerramentaQuery : IRequest<ArquivoDaFerramentaDto?>, IEscopoDaFerramenta
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    /// <summary>
    /// O arquivo final da peça do projeto no lugar do atual (o visualizador da peça); sem final, não há desenho.
    /// </summary>
    public bool Final { get; set; }
}

public sealed class GetArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetArquivoFerramentaQuery, ArquivoDaFerramentaDto?>
{
    public async Task<ArquivoDaFerramentaDto?> Handle(GetArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return null;
        }

        var arquivo = await ConversaDoEscopo.ArquivoAsync(unitOfWork, conversa.Id, request.Final, cancellationToken);
        return arquivo is null || arquivo.EmBranco ? null : new ArquivoDaFerramentaDto(arquivo.Id, arquivo.Conteudo, arquivo.Nome);
    }
}
