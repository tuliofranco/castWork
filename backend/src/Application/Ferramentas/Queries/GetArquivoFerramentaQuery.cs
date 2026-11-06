using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// O arquivo atual da ferramenta (o DXF do desenho), ou nulo se ainda não existe.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetArquivoFerramentaQuery : IRequest<ArquivoDaFerramentaDto?>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetArquivoFerramentaQuery, ArquivoDaFerramentaDto?>
{
    public async Task<ArquivoDaFerramentaDto?> Handle(GetArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return null;
        }

        var arquivo = await ConversaDaVersao.ArquivoAtualAsync(unitOfWork, conversa.Id, cancellationToken);
        return arquivo is null || arquivo.EmBranco ? null : new ArquivoDaFerramentaDto(arquivo.Id, arquivo.Conteudo);
    }
}
