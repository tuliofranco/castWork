using Castwork.Application.Abstractions;
using Castwork.Application.Projetos.Queries;
using Castwork.Domain.Entities.Assistente;

using GreenDonut;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Castwork.GraphQl.DataLoaders;

/// <summary>
/// O arquivo final de cada ferramenta de várias peças do projeto numa consulta só, sem o conteúdo.
/// </summary>
public sealed class ArquivosFinaisPorPecaDoProjetoDataLoader(IServiceScopeFactory scopes, IBatchScheduler batchScheduler, DataLoaderOptions options)
    : GroupedDataLoader<long, ArquivoFinalDaPecaDto>(batchScheduler, options)
{
    protected override async Task<ILookup<long, ArquivoFinalDaPecaDto>> LoadGroupedBatchAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var arquivos = unitOfWork.GetRepository<ArquivoFerramenta>().FindAll();
        var finais = await unitOfWork.GetRepository<ConversaFerramenta>()
            .FindBy(c => c.PecaDoProjetoId != null && keys.Contains(c.PecaDoProjetoId.Value) && c.ArquivoFinalId != null)
            .Join(arquivos, c => c.ArquivoFinalId, a => (long?)a.Id, (c, a) => new ArquivoFinalDaPecaDto
            {
                PecaDoProjetoId = c.PecaDoProjetoId!.Value,
                Ferramenta = c.Ferramenta,
                ArquivoId = a.Id,
                Nome = a.Nome,
                CriadoEm = a.CriadoEm,
            })
            .OrderBy(f => f.Ferramenta)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return finais.ToLookup(f => f.PecaDoProjetoId);
    }
}
