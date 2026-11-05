using Castwork.Domain.Common;

namespace Castwork.Domain.Entities.Projetos;

/// <summary>
/// Uma versão da arte (v1, v2...). Só é alterada pelo <see cref="Projeto"/>.
/// </summary>
public sealed partial class VersaoProjeto : BaseEntity<long>
{
    public const int NomeMaxLength = 100;
    public const int ObservacoesMaxLength = 2000;

    private readonly List<ItemComposicao> _itens = [];

    internal VersaoProjeto(int numero, string nome, string? observacoes)
    {
        Numero = numero;
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Observacoes = Guard.Opcional(observacoes, nameof(Observacoes), ObservacoesMaxLength);
        Status = StatusVersao.EmDesenvolvimento;
    }

    private VersaoProjeto()
    {
    }

    public int Numero { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string? Observacoes { get; private set; }

    public StatusVersao Status { get; private set; }

    public DateTimeOffset? PublicadaEm { get; private set; }

    public IReadOnlyCollection<ItemComposicao> Itens => _itens.AsReadOnly();
}
