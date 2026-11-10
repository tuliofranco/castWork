using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Pecas.Commands;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.CriarPecaDoProjeto;

/// <summary>
/// Cria uma peça no projeto: nasce em rascunho, sem vínculo com a biblioteca e sem valor. O material é uma liga ativa do
/// ateliê (o nome e o teor dela são copiados) ou, na pedra, texto livre. Retorna o Id da peça do projeto.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class CriarPecaDoProjetoCommand : ComandoDeProjeto, IDadosDaPeca, IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public TipoPeca Tipo { get; set; }

    /// <inheritdoc/>
    public long? LigaId { get; set; }

    /// <inheritdoc/>
    public string? Material { get; set; }

    public decimal? PesoGramas { get; set; }

    public string? Descricao { get; set; }
}

public sealed class CriarPecaDoProjetoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarPecaDoProjetoCommand, long>
{
    public async Task<long> Handle(CriarPecaDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var liga = await request.ObterLigaAsync(unitOfWork, cancellationToken);
        var peca = projeto.CriarPeca(request.Nome, request.Tipo, liga, request.Material, request.PesoDominio(), request.Descricao);
        await unitOfWork.SalvarAsync(cancellationToken);
        return peca.Id;
    }
}
