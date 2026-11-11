using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Application.Pecas.Commands;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

using MediatR;

namespace Castwork.Application.Projetos.Commands.AlterarPecaDoProjeto;

/// <summary>
/// Altera os dados da peça do projeto (o valor tem command próprio). Numa peça publicada, mudar qualquer dado faz dela
/// outra peça: volta a rascunho e perde o vínculo, e a peça da biblioteca não muda. Trocar a liga conta; a mesma liga
/// mantém a cópia do nome e do teor (ainda que desativada). Ativar ou desativar não conta.
/// </summary>
[Permissao(Regras.Projetos.Editar)]
public sealed class AlterarPecaDoProjetoCommand : ComandoDePecaDoProjeto, IDadosDaPeca, IRequest
{
    public string Nome { get; set; } = string.Empty;

    public TipoPeca Tipo { get; set; }

    /// <inheritdoc/>
    public long? LigaId { get; set; }

    /// <inheritdoc/>
    public string? Material { get; set; }

    public decimal? PesoGramas { get; set; }

    public string? Descricao { get; set; }

    /// <summary>
    /// Desativada não entra em composição nova. Padrão: ativa.
    /// </summary>
    public bool Ativo { get; set; } = true;
}

public sealed class AlterarPecaDoProjetoCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AlterarPecaDoProjetoCommand>
{
    public async Task Handle(AlterarPecaDoProjetoCommand request, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>().ObterPorIdOuFalharAsync(request.ProjetoId, cancellationToken);
        var liga = await request.ObterLigaAsync(unitOfWork, cancellationToken);
        projeto.AlterarPeca(
            request.PecaDoProjetoId,
            request.Nome,
            request.Tipo,
            liga,
            request.Material,
            request.PesoDominio(),
            request.Descricao,
            request.Ativo);
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
