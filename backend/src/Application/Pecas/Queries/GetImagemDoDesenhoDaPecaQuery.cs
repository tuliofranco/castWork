using Castwork.Application.Abstractions;
using Castwork.Application.Assistente;
using Castwork.Application.Assistente.Queries;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Pecas.Queries;

/// <summary>
/// Uma imagem guardada do desenho da peça da biblioteca, numa vista. Com <see cref="Formato"/> (<c>png</c> ou <c>svg</c>),
/// só naquele tipo; sem, a de melhor qualidade. Nula se não há.
/// </summary>
[Permissao(Regras.Pecas.Consultar)]
public sealed class GetImagemDoDesenhoDaPecaQuery : IRequest<ConteudoDaImagemDto?>
{
    public long PecaId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;

    public string Vista { get; set; } = string.Empty;

    public string? Formato { get; set; }
}

public sealed class GetImagemDoDesenhoDaPecaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetImagemDoDesenhoDaPecaQuery, ConteudoDaImagemDto?>
{
    public async Task<ConteudoDaImagemDto?> Handle(GetImagemDoDesenhoDaPecaQuery request, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.GetRepository<Peca>().ExisteAsync(request.PecaId, cancellationToken))
        {
            throw new EntidadeNaoEncontradaException("Peça", request.PecaId);
        }

        if (ImagensGuardadas.Tipos(request.Formato) is not { } tipos)
        {
            return null;
        }

        var imagens = await unitOfWork.GetRepository<Peca>()
            .FindBy(p => p.Id == request.PecaId)
            .SelectMany(p => p.Desenhos)
            .Where(d => d.Ferramenta == request.Ferramenta)
            .SelectMany(d => d.Imagens)
            .Where(i => i.Vista == request.Vista && tipos.Contains(i.Tipo))
            .Select(i => new ConteudoDaImagemDto(i.Conteudo, i.Tipo))
            .ToListAsync(cancellationToken);
        return imagens.OrderBy(i => tipos.IndexOf(i.Tipo)).FirstOrDefault();
    }
}
