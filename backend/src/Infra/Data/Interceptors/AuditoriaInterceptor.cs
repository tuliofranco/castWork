using Castwork.Application.Abstractions;
using Castwork.Domain.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Castwork.Infra.Data.Interceptors;

/// <summary>
/// Preenche criado/alterado em/por de toda entidade <see cref="IAuditavel"/> antes de salvar.
/// Fica no EF, e não nos handlers, para ser impossível esquecer: qualquer <c>SaveChanges</c> passa por aqui.
/// Também marca como alterada a raiz quando só um filho do agregado mudou (ex.: nova observação na peça).
/// </summary>
public sealed class AuditoriaInterceptor(ISessaoService sessao, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Auditar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Auditar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Filhos do agregado (coleções da raiz, ex.: etapas e observações da peça) adicionados ou alterados.
    /// </summary>
    private static bool TemFilhoAlterado(EntityEntry raiz)
        => raiz.Entity is IAggregateRoot
            && raiz.Collections
                .Where(c => c.CurrentValue is not null)
                .SelectMany(c => c.CurrentValue!.Cast<object>())
                .Where(filho => filho is not IAggregateRoot)
                .Any(filho => raiz.Context.Entry(filho).State is EntityState.Added or EntityState.Modified);

    private void Auditar(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var agora = timeProvider.GetUtcNow();
        var usuarioId = sessao.UsuarioId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditavel>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditavel.CriadoEm)).CurrentValue = agora;
                    entry.Property(nameof(IAuditavel.CriadoPor)).CurrentValue = usuarioId;
                    break;

                case EntityState.Modified:
                case EntityState.Unchanged when TemFilhoAlterado(entry):
                    entry.Property(nameof(IAuditavel.AlteradoEm)).CurrentValue = agora;
                    entry.Property(nameof(IAuditavel.AlteradoPor)).CurrentValue = usuarioId;

                    // Criação é imutável: mesmo que alguém altere por engano, o UPDATE não grava.
                    entry.Property(nameof(IAuditavel.CriadoEm)).IsModified = false;
                    entry.Property(nameof(IAuditavel.CriadoPor)).IsModified = false;
                    break;

                default:
                    break;
            }
        }
    }
}
