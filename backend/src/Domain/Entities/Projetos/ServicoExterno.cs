using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Serviço feito fora da bancada e somado ao custo da versão (fundição, cravação, ródio, gravação).
/// </summary>
public sealed record ServicoExterno
{
    public const int NomeMaxLength = 80;
    public const decimal ValorMaximo = 1_000_000m;

    public ServicoExterno(string nome, Dinheiro valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Valor = valor > Dinheiro.Zero && valor.Valor <= ValorMaximo
            ? valor
            : throw new ValorInvalidoException(nameof(Valor), $"deve ser maior que zero e até {ValorMaximo:N0}");
    }

    public string Nome { get; }

    public Dinheiro Valor { get; }
}
