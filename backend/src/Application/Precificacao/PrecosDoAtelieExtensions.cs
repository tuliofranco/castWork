using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Atelie;
using Castwork.Domain.Entities.Precificacao;

namespace Castwork.Application.Precificacao;

public static class PrecosDoAtelieExtensions
{
    /// <summary>
    /// Os padrões de preço do ateliê agora: a cotação vigente de cada metal que já tem cotação e os parâmetros.
    /// É o que um projeto copia (ao nascer ou ao atualizar para os valores atuais).
    /// </summary>
    public static async Task<PrecosDoAtelie> ObterPrecosDoAtelieAsync(this IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var cotacoes = new List<CotacaoMetal>();
        foreach (var metal in Enum.GetValues<Metal>())
        {
            var vigente = await unitOfWork.GetRepository<CotacaoMetal>()
                .FindBy(c => c.Metal == metal)
                .MaisRecentesPrimeiro()
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
            if (vigente is not null)
            {
                cotacoes.Add(vigente);
            }
        }

        var parametros = await unitOfWork.GetRepository<ConfiguracaoAtelie>()
            .FindBy(c => c.Id == ConfiguracaoAtelie.IdUnico)
            .Select(c => c.ParametrosDePreco)
            .SingleAsync(cancellationToken);

        return new PrecosDoAtelie(cotacoes, parametros);
    }
}
