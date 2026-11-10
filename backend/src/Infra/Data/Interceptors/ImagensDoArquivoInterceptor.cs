using Castwork.Application.Abstractions;
using Castwork.Domain.Entities.Assistente;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Castwork.Infra.Data.Interceptors;

/// <summary>
/// Todo arquivo novo das ferramentas (salvar, a IA, desfazer, restaurar, a conferência, a montagem, a importação) entra
/// na <see cref="IFilaDeImagens"/> depois de ir para o banco, para as imagens dele serem geradas em segundo plano. Fica no
/// EF, e não em cada command, para nenhum caminho que cria arquivo ficar sem imagens. O desenho em branco não entra.
/// </summary>
public sealed class ImagensDoArquivoInterceptor(IFilaDeImagens fila) : SaveChangesInterceptor
{
    private readonly List<ArquivoFerramenta> _novos = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Anotar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Anotar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Enfileirar();
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Enfileirar();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _novos.Clear();
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _novos.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Anotar(DbContext? context)
    {
        _novos.Clear();
        if (context is not null)
        {
            _novos.AddRange(context.ChangeTracker.Entries<ArquivoFerramenta>()
                .Where(e => e.State == EntityState.Added && !e.Entity.EmBranco)
                .Select(e => e.Entity));
        }
    }

    private void Enfileirar()
    {
        foreach (var arquivo in _novos)
        {
            fila.Enfileirar(arquivo.Id);
        }

        _novos.Clear();
    }
}
