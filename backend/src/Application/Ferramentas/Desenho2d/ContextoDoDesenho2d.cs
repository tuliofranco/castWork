using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Entities.Pecas;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Ferramentas.Desenho2d;

/// <summary>
/// O que o desenho 2D conta para a IA sobre o trabalho: o projeto, a versão com a composição e o briefing
/// (o que será feito). As instruções da IA ficam na Infra (Ferramentas/Desenho2d).
/// </summary>
internal static class ContextoDoDesenho2d
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static async Task<string> MontarAsync(IUnitOfWork unitOfWork, long projetoId, long versaoId, CancellationToken cancellationToken)
    {
        var projeto = await unitOfWork.GetRepository<Projeto>()
            .FindBy(p => p.Id == projetoId)
            .Include(p => p.Versoes).ThenInclude(v => v.Itens)
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        var versao = projeto.Versoes.Single(v => v.Id == versaoId);

        var pecaIds = versao.Itens.Select(i => i.PecaId).ToList();
        var pecas = await unitOfWork.GetRepository<Peca>()
            .FindBy(p => pecaIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Tipo, p.Material, p.Teor })
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var briefing = await unitOfWork.GetRepository<Briefing>()
            .FindBy(b => b.ProjetoId == projetoId)
            .Select(b => new
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
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return JsonSerializer.Serialize(
            new
            {
                projeto = projeto.Nome,
                versao = new { versao.Numero, versao.Nome, versao.Observacoes, status = versao.Status.ToString() },
                composicao = versao.Itens.Select(i => new
                {
                    peca = pecas.TryGetValue(i.PecaId, out var p) ? p.Nome : $"peça {i.PecaId}",
                    tipo = pecas.GetValueOrDefault(i.PecaId)?.Tipo.ToString(),
                    material = pecas.GetValueOrDefault(i.PecaId)?.Material,
                    i.Quantidade,
                    i.Observacao,
                }),
                briefing,
            },
            Json);
    }
}
