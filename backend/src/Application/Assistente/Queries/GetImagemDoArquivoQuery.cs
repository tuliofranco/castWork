using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// Uma imagem guardada do arquivo atual (ou do final, <see cref="Final"/>) da ferramenta, numa vista. Com <see cref="Formato"/> (<c>png</c> ou <c>svg</c>), só
/// naquele tipo; sem, a de melhor qualidade (o SVG do 2D, quando existe). Nula se não há.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetImagemDoArquivoQuery : IRequest<ConteudoDaImagemDto?>, IEscopoDaFerramenta
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public long PecaDoProjetoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public string Vista { get; set; } = string.Empty;

    public string? Formato { get; set; }

    /// <summary>
    /// O arquivo final da peça do projeto no lugar do atual (o visualizador da peça); sem final, não há desenho.
    /// </summary>
    public bool Final { get; set; }
}

public sealed class GetImagemDoArquivoQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagemDoArquivoQuery, ConteudoDaImagemDto?>
{
    public async Task<ConteudoDaImagemDto?> Handle(GetImagemDoArquivoQuery request, CancellationToken cancellationToken)
    {
        if (await ImagensGuardadas.ArquivoAsync(unitOfWork, request, request.Ferramenta, request.Final, cancellationToken) is not { } arquivoId
            || ImagensGuardadas.Tipos(request.Formato) is not { } tipos)
        {
            return null;
        }

        var vista = request.Vista;
        var imagens = await unitOfWork.GetRepository<ImagemDoArquivo>()
            .FindBy(i => i.ArquivoId == arquivoId && i.Vista == vista && tipos.Contains(i.Tipo))
            .Select(i => new ConteudoDaImagemDto(i.Conteudo, i.Tipo))
            .ToListAsync(cancellationToken);
        return imagens.OrderBy(i => tipos.IndexOf(i.Tipo)).FirstOrDefault();
    }
}
