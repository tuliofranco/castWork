using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// As imagens guardadas do arquivo atual (ou do final, <see cref="Final"/>) da ferramenta (vista e tipo, sem o conteúdo), na ordem de mostrar. Vazia se não
/// há desenho ou se as imagens ainda não foram geradas: a tela usa a prévia sob demanda.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetImagensDoArquivoQuery : IRequest<ImagensDoArquivoDto>, IEscopoDaFerramenta
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

/// <summary>
/// O arquivo atual (nulo sem desenho) e as imagens guardadas dele.
/// </summary>
public sealed record ImagensDoArquivoDto(long? ArquivoId, IReadOnlyList<ImagemGuardadaDto> Imagens);

/// <summary>
/// Uma imagem guardada: a vista (<c>iso</c>, <c>frente</c>, <c>topo</c>, <c>lado</c> ou <c>2d</c>) e o tipo (PNG ou SVG).
/// </summary>
public sealed record ImagemGuardadaDto(string Vista, string Tipo);

/// <summary>
/// O conteúdo de uma imagem guardada e o tipo dele.
/// </summary>
public sealed record ConteudoDaImagemDto(byte[] Conteudo, string Tipo);

public sealed class GetImagensDoArquivoQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagensDoArquivoQuery, ImagensDoArquivoDto>
{
    public async Task<ImagensDoArquivoDto> Handle(GetImagensDoArquivoQuery request, CancellationToken cancellationToken)
    {
        var arquivoId = await ImagensGuardadas.ArquivoAsync(unitOfWork, request, request.Ferramenta, request.Final, cancellationToken);
        if (arquivoId is not { } id)
        {
            return new ImagensDoArquivoDto(null, []);
        }

        var imagens = await unitOfWork.GetRepository<ImagemDoArquivo>()
            .FindBy(i => i.ArquivoId == id)
            .Select(i => new ImagemGuardadaDto(i.Vista, i.Tipo))
            .ToListAsync(cancellationToken);
        return new ImagensDoArquivoDto(id, ImagensGuardadas.Ordenar(imagens, i => i.Vista, i => i.Tipo));
    }
}
