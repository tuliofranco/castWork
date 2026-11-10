using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Ações da <see cref="Peca"/>.
/// </summary>
public sealed partial class Peca
{
    public static Peca Criar(string nome, TipoPeca tipo, string? material, TeorMetal? teor, Peso? peso, string? descricao, Dinheiro? custo)
    {
        var peca = new Peca { Ativo = true };
        peca.Atualizar(nome, tipo, material, teor, peso, descricao);
        peca.DefinirCusto(custo);
        return peca;
    }

    public void Atualizar(string nome, TipoPeca tipo, string? material, TeorMetal? teor, Peso? peso, string? descricao)
    {
        Nome = Guard.Obrigatorio(nome, nameof(Nome), NomeMaxLength);
        Tipo = Enum.IsDefined(tipo) ? tipo : throw new ValorInvalidoException(nameof(Tipo), "não é um tipo de peça válido");
        Material = Guard.Opcional(material, nameof(Material), MaterialMaxLength);
        Teor = teor;
        Peso = peso;
        Descricao = Guard.Opcional(descricao, nameof(Descricao), DescricaoMaxLength);
    }

    /// <summary>
    /// Custo por unidade (nulo = ainda sem custo). Separado de <see cref="Atualizar"/> porque só quem consulta a
    /// precificação pode vê-lo e, portanto, alterá-lo.
    /// </summary>
    public void DefinirCusto(Dinheiro? custo) => Custo = custo;

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;
}
