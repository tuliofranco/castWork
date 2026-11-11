using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Ferramentas.Modelo3d;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.Entities.Projetos;
using Castwork.Domain.Exceptions;

using MediatR;

namespace Castwork.Application.Projetos.Queries.GetPesoDaPeca;

/// <summary>
/// O peso da peça do projeto e de onde ele vem. Com modelo 3D, é o medido no arquivo final do Modelo 3D da peça (sem IA,
/// com a medição guardada pelo arquivo), todo na liga da peça (<see cref="PesoDoModelo.PesoNaLiga"/>); sem 3D final, vale o peso
/// digitado. Peça com 3D e sem liga (pedra ou material livre) fica com o digitado; se não foi possível medir, sem peso.
/// </summary>
[Permissao(Regras.Projetos.Consultar)]
public sealed class GetPesoDaPecaQuery : IRequest<PesoDaPecaDto>
{
    public long PecaDoProjetoId { get; set; }
}

public sealed class GetPesoDaPecaQueryHandler(IUnitOfWork unitOfWork, IMedidorDoModelo medidor) : IRequestHandler<GetPesoDaPecaQuery, PesoDaPecaDto>
{
    public async Task<PesoDaPecaDto> Handle(GetPesoDaPecaQuery request, CancellationToken cancellationToken)
    {
        var peca = await unitOfWork.GetRepository<Projeto>()
            .FindAll()
            .SelectMany(p => p.Pecas)
            .Where(p => p.Id == request.PecaDoProjetoId)
            .Select(p => new { p.Peso, p.LigaId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Peça do projeto", request.PecaDoProjetoId);
        var digitado = peca.Peso?.Gramas;

        var modelo = await PesoDoModelo.MedirArquivoFinalDaPecaAsync(unitOfWork, medidor, request.PecaDoProjetoId, cancellationToken);
        var ligaId = peca.LigaId;
        var densidade = modelo is null || ligaId is null
            ? null
            : await unitOfWork.GetRepository<Liga>().FindBy(l => l.Id == ligaId).Select(l => (decimal?)l.Densidade).SingleOrDefaultAsync(cancellationToken);

        return (modelo, densidade) switch
        {
            (null, _) => new PesoDaPecaDto(request.PecaDoProjetoId, digitado, OrigemDoPesoDaPeca.Digitado, digitado),
            (_, null) => new PesoDaPecaDto(request.PecaDoProjetoId, digitado, OrigemDoPesoDaPeca.ModeloSemLiga, digitado),
            ({ Medicao: null }, _) => new PesoDaPecaDto(request.PecaDoProjetoId, null, OrigemDoPesoDaPeca.ModeloNaoMedido, digitado),
            ({ Medicao: { } medicao }, { } d) => new PesoDaPecaDto(request.PecaDoProjetoId, PesoDoModelo.PesoNaLiga(medicao, d).Gramas, OrigemDoPesoDaPeca.Modelo, digitado),
        };
    }
}
