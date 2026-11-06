using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Ferramentas;

using MediatR;

namespace Castwork.Application.Ferramentas.Queries;

/// <summary>
/// As versões do arquivo da ferramenta (sem as arquivadas), da mais recente para a mais antiga, sem o conteúdo.
/// A primeira é a atual.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetHistoricoArquivoFerramentaQuery : IRequest<IReadOnlyList<VersaoDoArquivoDto>>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    public string Ferramenta { get; set; } = string.Empty;
}

public sealed class GetHistoricoArquivoFerramentaQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetHistoricoArquivoFerramentaQuery, IReadOnlyList<VersaoDoArquivoDto>>
{
    public async Task<IReadOnlyList<VersaoDoArquivoDto>> Handle(GetHistoricoArquivoFerramentaQuery request, CancellationToken cancellationToken)
    {
        await ConversaDaVersao.GarantirVersaoAsync(unitOfWork, request.ProjetoId, request.VersaoId, cancellationToken);
        var conversa = await ConversaDaVersao.ObterAsync(unitOfWork, request.ProjetoId, request.VersaoId, request.Ferramenta, cancellationToken);
        if (conversa is null)
        {
            return [];
        }

        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => a.ConversaId == conversa.Id && a.ArquivadoEm == null)
            .OrderByDescending(a => a.Id)
            .Select(a => new VersaoDoArquivoDto(a.Id, a.Origem, a.CriadoEm, a.Conteudo.Length, a.Nome))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
