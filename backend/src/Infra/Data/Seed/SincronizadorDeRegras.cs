using Castwork.Application.Common.Security;
using Castwork.Domain.Entities.Acesso;

using Microsoft.EntityFrameworkCore;

namespace Castwork.Infra.Data.Seed;

/// <summary>
/// Deixa a tabela <c>regras</c> igual ao catálogo em código: insere as novas, atualiza descrições e remove
/// as que saíram do catálogo (junto com o vínculo nos perfis). Idempotente; roda em toda inicialização.
/// </summary>
public static class SincronizadorDeRegras
{
    public static async Task<Resultado> SincronizarAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var catalogo = CatalogoDeRegras.Todas.ToDictionary(r => r.Id);
        var existentes = await context.Regras.ToListAsync(cancellationToken);

        var obsoletas = existentes.Where(r => !catalogo.ContainsKey(r.Id)).ToList();
        var idsObsoletos = obsoletas.Select(r => r.Id).ToList();
        if (idsObsoletos.Count > 0)
        {
            await context.Set<PerfilRegra>().Where(pr => idsObsoletos.Contains(pr.RegraId)).ExecuteDeleteAsync(cancellationToken);
            context.Regras.RemoveRange(obsoletas);
        }

        var atualizadas = 0;
        foreach (var existente in existentes.Where(r => catalogo.ContainsKey(r.Id)))
        {
            var doCatalogo = catalogo[existente.Id];
            if (existente.Codigo != doCatalogo.Codigo || existente.Grupo != doCatalogo.Grupo || existente.Descricao != doCatalogo.Descricao)
            {
                existente.Atualizar(doCatalogo.Codigo, doCatalogo.Grupo, doCatalogo.Descricao);
                atualizadas++;
            }
        }

        var idsExistentes = existentes.Select(r => r.Id).ToHashSet();
        var novas = catalogo.Values.Where(r => !idsExistentes.Contains(r.Id)).Select(r => new Regra(r.Id, r.Codigo, r.Grupo, r.Descricao)).ToList();
        context.Regras.AddRange(novas);

        await context.SaveChangesAsync(cancellationToken);
        return new Resultado(novas.Count, atualizadas, obsoletas.Count);
    }

    public sealed record Resultado(int Inseridas, int Atualizadas, int Removidas);
}
