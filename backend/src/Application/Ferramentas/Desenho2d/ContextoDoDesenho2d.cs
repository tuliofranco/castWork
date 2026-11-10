using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Ferramentas.Desenho2d;

/// <summary>
/// O que o desenho 2D conta para a IA sobre o trabalho: o projeto, a versão com a composição (ou, no desenho de uma peça
/// do projeto, a peça) e o briefing (o que será feito). As instruções da IA ficam na Infra (Ferramentas/Desenho2d).
/// </summary>
internal static class ContextoDoDesenho2d
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static async Task<string> MontarAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId)
            .Include(p => p.Versoes).ThenInclude(v => v.Itens)
            .Include(p => p.Pecas)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        var versao = projeto.Versoes.Single(v => v.Id == versaoId);

        var pecas = projeto.Pecas.ToDictionary(p => p.Id);

        return JsonSerializer.Serialize(
            new
            {
                projeto = projeto.Nome,
                versao = new { versao.Numero, versao.Nome, versao.Observacoes, status = versao.Status.ToString() },
                composicao = versao.Itens.Select(i => new
                {
                    peca = pecas.TryGetValue(i.PecaDoProjetoId, out var p) ? p.Nome : $"peça {i.PecaDoProjetoId}",
                    tipo = pecas.GetValueOrDefault(i.PecaDoProjetoId)?.Tipo.ToString(),
                    material = pecas.GetValueOrDefault(i.PecaDoProjetoId)?.Material,
                    i.Quantidade,
                    i.Observacao,
                }),
                briefing = await BriefingAsync(unitOfWork, projetoId, cancellationToken),
            },
            Json);
    }

    /// <summary>
    /// O desenho de uma peça do projeto (um elo, um fecho...): a peça (tipo, nome, liga e teor, peso informado, descrição)
    /// e o briefing do projeto.
    /// </summary>
    public static async Task<string> MontarDaPecaAsync(IUnitOfWork unitOfWork, long projetoId, long pecaDoProjetoId, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId)
            .Include(p => p.Pecas.Where(peca => peca.Id == pecaDoProjetoId))
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        var peca = projeto.Pecas.Single();

        return JsonSerializer.Serialize(
            new
            {
                projeto = projeto.Nome,
                peca = new
                {
                    tipo = peca.Tipo.ToString(),
                    peca.Nome,
                    liga = peca.Material,
                    teor = peca.Teor?.Milesimos,
                    pesoInformadoGramas = peca.Peso?.Gramas,
                    peca.Descricao,
                    status = peca.Status.ToString(),
                },
                briefing = await BriefingAsync(unitOfWork, projetoId, cancellationToken),
            },
            Json);
    }

    private static Task<object?> BriefingAsync(IUnitOfWork unitOfWork, long projetoId, CancellationToken cancellationToken)
        => unitOfWork.GetRepository<Briefing>()
            .FindBy(b => b.ProjetoId == projetoId)
            .Select(b => (object)new
            {
                b.TipoJoia,
                OQueSeraFeito = b.OQueSeraFeito ?? b.OQueOClienteQuer,
                b.Materiais,
                b.Medidas,
                b.Acabamento,
                b.Pedras,
                b.Fecho,
                b.Observacoes,
            })
            .SingleOrDefaultAsync(cancellationToken);
}
