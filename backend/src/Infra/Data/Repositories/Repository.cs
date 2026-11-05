using System.Linq.Expressions;

using Castwork.Application.Abstractions;
using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.Specifications;

using Microsoft.EntityFrameworkCore;

namespace Castwork.Infra.Data.Repositories;

public sealed class Repository<T>(ApplicationDbContext context) : IRepository<T>
    where T : BaseEntity<long>, IAggregateRoot
{
    private readonly DbSet<T> _set = context.Set<T>();

    public IQueryable<T> FindAll() => _set;

    public IQueryable<T> FindBy(Expression<Func<T, bool>> criterio) => _set.Where(criterio);

    public IQueryable<T> FindBy(BaseSpecification<T> specification) => SpecificationEvaluator.Aplicar(_set, specification);

    public IQueryable<TResult> FindBy<TResult>(BaseSpecification<T, TResult> specification)
        => SpecificationEvaluator.Aplicar(_set, specification);

    /// <summary>
    /// Carrega o agregado completo (com os filhos), porque é o que um command vai alterar.
    /// <c>AsSplitQuery</c> evita o produto cartesiano entre coleções irmãs (etapas × observações).
    /// </summary>
    public Task<T?> ObterPorIdAsync(long id, CancellationToken cancellationToken)
        => IncluirFilhosDoAgregado(_set).AsSplitQuery().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<T> ObterPorIdOuFalharAsync(long id, CancellationToken cancellationToken)
        => await ObterPorIdAsync(id, cancellationToken) ?? throw new EntidadeNaoEncontradaException(typeof(T).Name, id);

    public Task<bool> ExisteAsync(long id, CancellationToken cancellationToken)
        => _set.AnyAsync(e => e.Id == id, cancellationToken);

    public void Adicionar(T entidade) => _set.Add(entidade);

    public void Remover(T entidade) => _set.Remove(entidade);

    private static IEnumerable<string> CaminhosDosFilhos(Microsoft.EntityFrameworkCore.Metadata.IEntityType tipo, string? prefixo)
        => tipo.GetNavigations()
            .Where(n => n.IsCollection && !typeof(IAggregateRoot).IsAssignableFrom(n.TargetEntityType.ClrType))
            .SelectMany(n =>
            {
                var caminho = prefixo is null ? n.Name : $"{prefixo}.{n.Name}";
                var netos = CaminhosDosFilhos(n.TargetEntityType, caminho).ToList();
                return netos.Count == 0 ? [caminho] : netos;
            });

    /// <summary>
    /// Inclui as coleções que pertencem ao agregado, em qualquer nível (ex.: Projeto → Versoes → Itens).
    /// Navegações para outros agregados (ex.: Item → Peça) ficam de fora de propósito.
    /// </summary>
    private IQueryable<T> IncluirFilhosDoAgregado(IQueryable<T> query)
        => CaminhosDosFilhos(context.Model.FindEntityType(typeof(T))!, prefixo: null)
            .Aggregate(query, (atual, caminho) => atual.Include(caminho));
}
