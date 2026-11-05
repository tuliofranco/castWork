using Castwork.Application.Abstractions;
using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.ValueObjects;

using MediatR;

namespace Castwork.Application.Pecas.Commands.CriarPeca;

/// <summary>
/// Cadastra uma peça na biblioteca. Retorna o Id.
/// </summary>
[Permissao(Regras.Pecas.Criar)]
public sealed class CriarPecaCommand : IRequest<long>
{
    public string Nome { get; set; } = string.Empty;

    public TipoPeca Tipo { get; set; }

    public string? Material { get; set; }

    /// <summary>
    /// Teor do metal: "18k" ou "750". Opcional.
    /// </summary>
    public string? Teor { get; set; }

    public decimal? PesoGramas { get; set; }

    public string? Descricao { get; set; }
}

public sealed class CriarPecaCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CriarPecaCommand, long>
{
    public async Task<long> Handle(CriarPecaCommand request, CancellationToken cancellationToken)
    {
        var peca = Peca.Criar(
            request.Nome,
            request.Tipo,
            request.Material,
            string.IsNullOrWhiteSpace(request.Teor) ? null : TeorMetal.Parse(request.Teor),
            request.PesoGramas is null ? null : Peso.EmGramas(request.PesoGramas.Value),
            request.Descricao);

        unitOfWork.GetRepository<Peca>().Adicionar(peca);
        await unitOfWork.SalvarAsync(cancellationToken);

        return peca.Id;
    }
}
