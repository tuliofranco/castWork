using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// A prévia (SVG) de um arquivo da ferramenta (o atual, sem <see cref="ArquivoId"/>, ou o final da peça, com <see cref="Final"/>), para a pasta de arquivos
/// da versão. Nula se não há desenho ou se a prévia não pôde ser gerada.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetPreviaArquivoFerramentaQuery : IRequest<PreviaDoArquivoDto?>, IEscopoDaFerramenta
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public long? ArquivoId { get; set; }

    /// <summary>
    /// O arquivo final da peça do projeto no lugar do atual (o visualizador da peça); sem final, não há desenho.
    /// </summary>
    public bool Final { get; set; }
}

public sealed class GetPreviaArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork, IPreviaFerramenta previa)
    : IRequestHandler<GetPreviaArquivoFerramentaQuery, PreviaDoArquivoDto?>
{
    public async Task<PreviaDoArquivoDto?> Handle(GetPreviaArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, request, cancellationToken);
        var conversa = await ConversaDoEscopo.ObterAsync(unitOfWork, request, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return null;
        }

        var arquivo = request.ArquivoId is { } id
            ? await unitOfWork.GetRepository<ArquivoFerramenta>().FindBy(a => a.Id == id && a.ConversaId == conversa.Id).AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            : await ConversaDoEscopo.ArquivoAsync(unitOfWork, conversa.Id, request.Final, cancellationToken);
        if (arquivo is null || arquivo.EmBranco)
        {
            return null;
        }

        var svg = await previa.GerarAsync(request.Ferramenta, arquivo.Conteudo, cancellationToken);
        return svg is null ? null : new PreviaDoArquivoDto(arquivo.Id, svg);
    }
}
