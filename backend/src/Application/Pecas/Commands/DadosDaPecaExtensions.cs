using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Precificacao;
using Castwork.Domain.ValueObjects;

namespace Castwork.Application.Pecas.Commands;

/// <summary>
/// Converte os campos validados de <see cref="IDadosDaPeca"/> no que o domínio recebe.
/// </summary>
internal static class DadosDaPecaExtensions
{
    public static Peso? PesoDominio(this IDadosDaPeca dados)
        => dados.PesoGramas is null ? null : Peso.EmGramas(dados.PesoGramas.Value);

    /// <summary>
    /// A liga informada (ativa ou não: o domínio decide se a aceita), ou nula sem liga.
    /// </summary>
    public static async Task<Liga?> ObterLigaAsync(this IDadosDaPeca dados, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
        => dados.LigaId is long id ? await unitOfWork.GetRepository<Liga>().ObterPorIdOuFalharAsync(id, cancellationToken) : null;
}
