using System.Linq.Expressions;

namespace Castwork.Domain.Specifications;

/// <summary>
/// Consulta reutilizável descrita em termos de domínio ("peças em andamento do cliente X").
/// O domínio só descreve critério, includes e ordenação; quem traduz para IQueryable é o
/// <c>SpecificationEvaluator</c> da Infra.Data.
/// </summary>
public abstract class BaseSpecification<T>(Expression<Func<T, bool>>? criterio = null)
    where T : class
{
    private readonly List<Expression<Func<T, object>>> _includes = [];

    public Expression<Func<T, bool>>? Criterio { get; } = criterio;

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    public Expression<Func<T, object>>? OrdenarPor { get; private set; }

    public Expression<Func<T, object>>? OrdenarPorDescendente { get; private set; }

    /// <summary>
    /// Avalia em memória (útil em regras de domínio e testes). Compila a expressão a cada chamada;
    /// não usar em laços grandes.
    /// </summary>
    public bool EhSatisfeitaPor(T entidade) => Criterio?.Compile().Invoke(entidade) ?? true;

    protected void Incluir(Expression<Func<T, object>> include) => _includes.Add(include);

    protected void Ordenar(Expression<Func<T, object>> chave) => OrdenarPor = chave;

    protected void OrdenarDescendente(Expression<Func<T, object>> chave) => OrdenarPorDescendente = chave;
}

/// <summary>
/// Specification com projeção: o banco devolve só as colunas de <typeparamref name="TResult"/>.
/// </summary>
public abstract class BaseSpecification<T, TResult>(
    Expression<Func<T, TResult>> projecao,
    Expression<Func<T, bool>>? criterio = null)
    : BaseSpecification<T>(criterio)
    where T : class
{
    public Expression<Func<T, TResult>> Projecao { get; } = projecao;
}
