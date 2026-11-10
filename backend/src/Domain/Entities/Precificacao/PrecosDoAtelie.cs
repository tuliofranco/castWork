using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Os padrões de preço do ateliê num instante: a cotação vigente de cada metal (só os que já têm cotação) e os
/// parâmetros. É o que o projeto copia ao nascer ou ao "atualizar para os valores atuais". Não é gravado.
/// </summary>
public sealed class PrecosDoAtelie
{
    public PrecosDoAtelie(IEnumerable<CotacaoMetal> cotacoesVigentes, ParametrosDePreco parametros)
    {
        ArgumentNullException.ThrowIfNull(cotacoesVigentes);
        ArgumentNullException.ThrowIfNull(parametros);
        CotacoesVigentes = [.. cotacoesVigentes];
        Parametros = parametros;
    }

    public IReadOnlyList<CotacaoMetal> CotacoesVigentes { get; }

    public ParametrosDePreco Parametros { get; }
}
