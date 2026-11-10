using Castwork.Application.Abstractions;
using Castwork.Application.Common.Pagination;
using Castwork.Application.Common.Pagination.Extensions;
using Castwork.Application.Common.Pagination.Models;
using Castwork.Application.Common.Security;
using Castwork.Application.Common.Validators;
using Castwork.Domain.Entities.Assistente;

using MediatR;

namespace Castwork.Application.Assistente.Queries;

/// <summary>
/// Os arquivos da versão, de todas as ferramentas (a pasta da Composição): cada versão gravada do arquivo,
/// da mais recente para a mais antiga. O desenho em branco do Desfazer não é arquivo e fica de fora.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetArquivosDaVersaoQuery : QueryPaginadaPara<ArquivoDaVersaoDto>
{
    public long ProjetoId { get; set; }

    public long VersaoId { get; set; }

    /// <summary>
    /// Lista os arquivados (a aba "Arquivados") em vez dos ativos.
    /// </summary>
    public bool Arquivados { get; set; }
}

public sealed class GetArquivosDaVersaoQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetArquivosDaVersaoQuery, ListaPaginada<ArquivoDaVersaoDto>>
{
    public async Task<ListaPaginada<ArquivoDaVersaoDto>> Handle(GetArquivosDaVersaoQuery request, CancellationToken cancellationToken)
    {
        await ConversaDoEscopo.GarantirEscopoAsync(unitOfWork, EscopoDaFerramenta.DaVersao(request.ProjetoId, request.VersaoId), cancellationToken);
        var conversas = unitOfWork.GetRepository<ConversaFerramenta>().FindBy(c => c.VersaoId == request.VersaoId);

        return await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => conversas.Any(c => c.Id == a.ConversaId) && (a.ArquivadoEm != null) == request.Arquivados)
            .Select(a => new ArquivoDaVersaoDto
            {
                Id = a.Id,
                Ferramenta = conversas.Where(c => c.Id == a.ConversaId).Select(c => c.Ferramenta).First(),
                Origem = a.Origem,
                CriadoEm = a.CriadoEm,
                Tamanho = a.Conteudo.Length,
                Nome = a.Nome,
                ArquivadoEm = a.ArquivadoEm,
            })
            .Where(a => a.Tamanho > 0)
            .ToListaPaginada(request)
            .OrderByDesc(a => a.Id)
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetArquivosDaVersaoQueryValidator(IUnitOfWork unitOfWork)
    : ValidatorPaginado<GetArquivosDaVersaoQuery, ArquivoDaVersaoDto>(unitOfWork);
