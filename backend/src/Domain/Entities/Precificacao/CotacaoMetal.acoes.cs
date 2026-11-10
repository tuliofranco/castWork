using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Precificacao;

/// <summary>
/// Ações da <see cref="CotacaoMetal"/>.
/// </summary>
public sealed partial class CotacaoMetal
{
    public static CotacaoMetal Registrar(Metal metal, Dinheiro valor, DateTimeOffset registradaEm, long? registradaPor)
        => new()
        {
            Metal = Enum.IsDefined(metal) ? metal : throw new ValorInvalidoException(nameof(Metal), "não é um metal válido"),
            Valor = valor > Dinheiro.Zero ? valor : throw new ValorInvalidoException(nameof(Valor), "deve ser maior que zero"),
            RegistradaEm = registradaEm,
            RegistradaPor = registradaPor,
        };
}
