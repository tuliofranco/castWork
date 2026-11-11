using Castwork.Application.Precificacao.Queries.GetPrecosDoAtelie;
using Castwork.Application.Projetos.Queries;
using Castwork.Domain.Entities.Clientes;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Projetos;

public static class ProjetosQueryExtensions
{
    /// <summary>
    /// Busca por trecho do nome do projeto ou do cliente, sem diferenciar maiúsculas.
    /// Projeto e Cliente são agregados distintos, por isso o cliente entra como subconsulta.
    /// </summary>
    public static IQueryable<Projeto> ComTermo(this IQueryable<Projeto> query, string? termo, IQueryable<Cliente> clientes)
    {
        if (string.IsNullOrWhiteSpace(termo))
        {
            return query;
        }

#pragma warning disable CA1304, CA1311, CA1862 // Expressão traduzida para SQL pelo EF; cultura e StringComparison não se aplicam.
        var termoMinusculo = termo.Trim().ToLower();
        return query.Where(p => p.Nome.ToLower().Contains(termoMinusculo)
            || clientes.Any(c => c.Id == p.ClienteId && c.Nome.ToLower().Contains(termoMinusculo)));
#pragma warning restore CA1304, CA1311, CA1862
    }

    /// <summary>
    /// Preços de cada projeto (cotações e parâmetros) numa consulta só. Os valores saem dos value objects na projeção.
    /// </summary>
    public static IQueryable<PrecosDoProjetoDto> ParaPrecosDto(this IQueryable<Projeto> query)
        => query.Select(p => new PrecosDoProjetoDto
        {
            ProjetoId = p.Id,
            Cotacoes = p.Cotacoes
                .OrderBy(c => c.Metal)
                .Select(c => new CotacaoDoProjetoDto { Metal = c.Metal, Valor = c.Valor.Valor, CotadaEm = c.CotadaEm })
                .ToList(),
            Parametros = new ParametrosDePrecoDto
            {
                PerdaPercentual = p.ParametrosDePreco.PerdaPercentual,
                ValorHora = p.ParametrosDePreco.ValorHora.Valor,
                Markup = p.ParametrosDePreco.Markup,
                TaxasPercentual = p.ParametrosDePreco.TaxasPercentual,
                ArredondarPara = p.ParametrosDePreco.ArredondarPara.Valor,
            },
            HerdadosEm = p.PrecosHerdadosEm,
            AlteradosEm = p.PrecosAlteradosEm,
        });
}
