namespace Castwork.Application.Common.Pagination.Models;

/// <summary>
/// Parâmetros de paginação recebidos do front. Valores fora da faixa são corrigidos, não rejeitados:
/// página &lt; 1 vira 1 e tamanho fora de 1..<see cref="TamanhoMaximo"/> vira o padrão.
/// </summary>
public abstract class QueryPaginada
{
    public const int TamanhoPadrao = 50;
    public const int TamanhoMaximo = 50;

    private int _pagina = 1;
    private int _tamanhoPagina = TamanhoPadrao;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanhoPagina
    {
        get => _tamanhoPagina;
        set => _tamanhoPagina = value is < 1 or > TamanhoMaximo ? TamanhoPadrao : value;
    }

    /// <summary>
    /// Nome de uma propriedade do DTO de retorno (sem diferenciar maiúsculas). Validado pelo
    /// <c>ValidatorPaginado</c>: campo inexistente é erro 400, não é ignorado.
    /// </summary>
    public string? OrdenarPor { get; set; }

    public bool Decrescente { get; set; } = true;
}
