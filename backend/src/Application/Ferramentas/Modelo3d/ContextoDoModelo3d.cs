using System.Text.Json;

using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;
using Castwork.Domain.Entities.Ferramentas.Briefings;
using Castwork.Domain.Entities.Projetos;

namespace Castwork.Application.Ferramentas.Modelo3d;

/// <summary>
/// O que o modelo 3D conta para a IA sobre o trabalho: o projeto, a versão com a composição (e quais peças têm modelo 3D,
/// que o "Montar a partir das peças" traz para a versão) ou, no modelo de uma peça do projeto, a peça; o briefing (o que
/// será feito) e os materiais do ateliê que o arquivo tem (<see cref="MateriaisDoModelo"/>). As instruções da IA ficam na
/// Infra (Ferramentas/Modelo3d).
/// </summary>
internal static class ContextoDoModelo3d
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static async Task<string> MontarAsync(
        IUnitOfWork unitOfWork,
        long projetoId,
        long versaoId,
        IReadOnlyList<MaterialDoModelo> materiais,
        CancellationToken cancellationToken)
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
        var comModelo = await PecasComModeloAsync(unitOfWork, [.. versao.Itens.Select(i => i.PecaDoProjetoId)], cancellationToken);

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
                    temModelo3d = comModelo.Contains(i.PecaDoProjetoId),
                }),
                briefing = await BriefingAsync(unitOfWork, projetoId, cancellationToken),
                materiais = materiais.Select(m => new { nome = m.Nome, m.Uso, densidade = m.Densidade }),
            },
            Json);
    }

    /// <summary>
    /// O modelo de uma peça do projeto (um elo, um fecho...): a peça (tipo, nome, liga e teor, peso informado, descrição),
    /// o material do arquivo que é a liga dela (quando a liga está ativa), o briefing do projeto e os materiais do ateliê.
    /// </summary>
    public static async Task<string> MontarDaPecaAsync(
        IUnitOfWork unitOfWork,
        long projetoId,
        long pecaDoProjetoId,
        IReadOnlyList<MaterialDoModelo> materiais,
        CancellationToken cancellationToken)
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
                    materialDaLiga = materiais.FirstOrDefault(m => m.LigaId is not null && m.LigaId == peca.LigaId)?.Nome,
                    pesoInformadoGramas = peca.Peso?.Gramas,
                    peca.Descricao,
                    status = peca.Status.ToString(),
                },
                briefing = await BriefingAsync(unitOfWork, projetoId, cancellationToken),
                materiais = materiais.Select(m => new { nome = m.Nome, m.Uso, densidade = m.Densidade }),
            },
            Json);
    }

    /// <summary>
    /// As peças do projeto que têm modelo 3D: a conversa do Modelo 3D da peça com um arquivo final que não está em branco.
    /// </summary>
    public static async Task<HashSet<long>> PecasComModeloAsync(IUnitOfWork unitOfWork, IReadOnlyCollection<long> pecaIds, CancellationToken cancellationToken)
    {
        var finais = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.PecaDoProjetoId != null && pecaIds.Contains(c.PecaDoProjetoId.Value) && c.Ferramenta == Modelo3dNoAssistente.Codigo && c.ArquivoFinalId != null)
            .Select(c => new { PecaId = c.PecaDoProjetoId!.Value, ArquivoId = c.ArquivoFinalId!.Value })
            .ToListAsync(cancellationToken);
        var ids = finais.Select(f => f.ArquivoId).ToList();
        var comDesenho = await unitOfWork.GetRepository<ArquivoFerramenta>()
            .FindBy(a => ids.Contains(a.Id) && a.Conteudo.Length > 0)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        return [.. finais.Where(f => comDesenho.Contains(f.ArquivoId)).Select(f => f.PecaId)];
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
