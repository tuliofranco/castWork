using Castwork.Domain.Common;
using Castwork.Domain.Exceptions;
using Castwork.Domain.ValueObjects;

namespace Castwork.Domain.Entities.Pecas;

/// <summary>
/// Ações da <see cref="Peca"/>.
/// </summary>
public sealed partial class Peca
{
    public static Peca Criar(string nome, TipoPeca tipo, string? material, TeorMetal? teor, Peso? peso, string? descricao)
    {
        var peca = new Peca { Ativo = true };
        peca.Atualizar(nome, tipo, material, teor, peso, descricao);
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

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;
}
