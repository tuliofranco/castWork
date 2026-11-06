using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Ferramentas;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// A prévia (SVG) de um arquivo da ferramenta (o atual, sem <see cref="ArquivoId"/>), para a pasta de arquivos
/// da versão. Nula se não há desenho ou se a prévia não pôde ser gerada.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetPreviaArquivoFerramentaQuery : IRequest<PreviaDoArquivoDto?>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public long? ArquivoId { get; set; }
}

public sealed class GetPreviaArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork, IPreviaFerramenta previa)
    : IRequestHandler<GetPreviaArquivoFerramentaQuery, PreviaDoArquivoDto?>
{
    public async Task<PreviaDoArquivoDto?> Handle(GetPreviaArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return null;
        }

        var arquivo = request.ArquivoId is { } id
            ? await unitOfWork.GetRepository<ArquivoFerramenta>().FindBy(a => a.Id == id && a.ConversaId == conversa.Id).AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            : await ConversaDaVersao.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        if (arquivo is null || arquivo.EmBranco)
        {
            return null;
        }

        var svg = await previa.GerarAsync(request.Ferramenta, arquivo.Conteudo, cancellationToken);
        return svg is null ? null : new PreviaDoArquivoDto(arquivo.Id, svg);
    }
}
