using Castwork.Domain.Exceptions;

namespace Castwork.Application.Assistente;

/// <summary>
/// As ferramentas registradas no Assistente (cada uma pela sua implementação de <see cref="IFerramentaComIa"/>).
/// </summary>
public sealed class CatalogoDeFerramentas(IEnumerable<IFerramentaComIa> ferramentas)
{
    private readonly Dictionary<string, IFerramentaComIa> _porCodigo = ferramentas.ToDictionary(f => f.Codigo);

    public bool Conhece(string codigo) => _porCodigo.ContainsKey(codigo);

    public IFerramentaComIa Obter(string codigo)
        => _porCodigo.TryGetValue(codigo, out var ferramenta) ? ferramenta : throw new EntidadeNaoEncontradaException("Ferramenta", codigo);
}
